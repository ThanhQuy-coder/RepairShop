using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.AIAdvisory.DTOs;

namespace RepairShop.Infrastructure.AI;

/// <summary>
/// Task 4.1 — Client gọi MAS (`POST /v1/agents/execute`) thay cho one-shot advisory.
///
/// Trách nhiệm:
/// 1. Dựng context THẬT từ PostgreSQL (services/parts/slots) — AI không truy cập DB.
/// 2. Gọi FastAPI bằng `X-Internal-Api-Key`, timeout cấu hình riêng.
/// 3. Hậu kiểm: ID phải thuộc context, giá lấy từ catalog, slot phải còn chỗ.
/// 4. Không throw: mọi lỗi AI => `AiAvailable=false` + thông điệp thân thiện (fallback).
/// </summary>
public class AIConsultationClient : IAIConsultationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const int MaxSlotDaysAhead = 3;

    private readonly HttpClient _httpClient;
    private readonly IServiceCatalogQueryService _serviceCatalog;
    private readonly ITimeSlotConfigRepository _slotRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IBusinessClock _clock;
    private readonly ILogger<AIConsultationClient> _logger;

    public AIConsultationClient(
        HttpClient httpClient,
        IServiceCatalogQueryService serviceCatalog,
        ITimeSlotConfigRepository slotRepository,
        IAppointmentRepository appointmentRepository,
        IBusinessClock clock,
        ILogger<AIConsultationClient> logger)
    {
        _httpClient = httpClient;
        _serviceCatalog = serviceCatalog;
        _slotRepository = slotRepository;
        _appointmentRepository = appointmentRepository;
        _clock = clock;
        _logger = logger;
    }

    public async Task<ConsultationResponse> GetConsultationAsync(
        ConsultationRequest request,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(_clock.Now.Date);
        if (request.RequestedDate is { } requestedDate && requestedDate < today)
        {
            return NoMatch("Ngày hẹn đã qua. Vui lòng chọn ngày khác.");
        }

        try
        {
            var services = await _serviceCatalog.GetAvailableServicesAsync(request.DeviceType);
            var parts = await _serviceCatalog.GetAvailablePartsAsync(request.DeviceType);
            var (slotDate, slots) = await BuildSlotsAsync(request.RequestedDate, today, cancellationToken);

            var payload = new
            {
                requestId = Guid.NewGuid(),
                actorType = "GUEST",
                message = request.IssueDescription.Trim(),
                device = new
                {
                    deviceType = request.DeviceType.ToLowerInvariant(),
                    brand = request.Brand.Trim(),
                    model = request.Model.Trim()
                },
                issueDescription = request.IssueDescription.Trim(),
                context = new
                {
                    availableServices = services.Select(service => new
                    {
                        serviceId = service.ServiceId,
                        name = service.Name,
                        description = service.Description,
                        deviceType = string.IsNullOrWhiteSpace(service.DeviceType)
                            ? request.DeviceType.ToLowerInvariant()
                            : service.DeviceType.ToLowerInvariant(),
                        basePrice = service.BasePrice
                    }),
                    availableParts = parts.Select(part => new
                    {
                        partId = part.PartId,
                        name = part.Name,
                        unitPrice = part.UnitPrice,
                        compatibleDeviceType = part.CompatibleDeviceType?.ToLowerInvariant(),
                        inStock = part.InStock
                    }),
                    availableSlots = slots.Select(slot => new
                    {
                        timeSlotId = slot.TimeSlotId.ToString(),
                        slotStart = slot.SlotStart.ToString("HH:mm"),
                        slotEnd = slot.SlotEnd.ToString("HH:mm"),
                        maxCapacity = slot.MaxCapacity,
                        booked = slot.Booked,
                        available = slot.Available,
                        isFull = slot.Available == 0,
                        appointmentDate = slot.Date.ToString("yyyy-MM-dd")
                    })
                },
                options = new
                {
                    allowDiagnosis = true,
                    allowQuotation = request.AllowQuotation,
                    allowAppointment = request.AllowAppointment
                },
                customerInfo = new
                {
                    fullName = request.FullName,
                    phone = request.Phone
                }
            };

            using var httpResponse = await _httpClient.PostAsJsonAsync(
                "/v1/agents/execute", payload, cancellationToken);

            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "AI consult trả HTTP {StatusCode} cho {DeviceType} {Brand}",
                    (int)httpResponse.StatusCode,
                    request.DeviceType,
                    request.Brand);
                return Unavailable();
            }

            var body = await httpResponse.Content.ReadFromJsonAsync<AiConsultResponse>(
                JsonOptions, cancellationToken);

            if (body is null)
            {
                _logger.LogError("AI consult trả body rỗng.");
                return Unavailable();
            }

            return MapResponse(request, body, services, parts, slots, slotDate);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "AI consult timeout.");
            return Unavailable();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Không kết nối được AI Service cho consult.");
            return Unavailable();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "AI consult trả JSON không hợp lệ.");
            return Unavailable();
        }
    }

    private async Task<(DateOnly Date, List<SlotContext> Slots)> BuildSlotsAsync(
        DateOnly? requestedDate,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var configurations = await _slotRepository.ListAsync();
        var dates = new List<DateOnly>();

        if (requestedDate is { } requested)
        {
            dates.Add(requested);
        }
        else
        {
            for (var offset = 1; offset <= MaxSlotDaysAhead; offset++)
            {
                dates.Add(today.AddDays(offset));
            }
        }

        foreach (var date in dates)
        {
            var slots = new List<SlotContext>();
            foreach (var config in configurations
                         .Where(config => config.IsActive && config.AppliesTo(date))
                         .OrderBy(config => config.SlotStart))
            {
                var booked = await _appointmentRepository.CountBookedAsync(date, config.Id);
                var available = Math.Max(0, config.MaxCapacity - booked);
                slots.Add(new SlotContext(
                    config.Id, date, config.SlotStart, config.SlotEnd,
                    config.MaxCapacity, booked, available));
            }

            if (slots.Count > 0 || requestedDate is not null)
            {
                return (date, slots);
            }
        }

        return (today.AddDays(1), []);
    }

    private ConsultationResponse MapResponse(
        ConsultationRequest request,
        AiConsultResponse body,
        List<CatalogServiceItem> services,
        List<CatalogPartItem> parts,
        List<SlotContext> slots,
        DateOnly slotDate)
    {
        var status = (body.Status ?? "ERROR").Trim().ToUpperInvariant();
        var intent = (body.Intent ?? "UNKNOWN").Trim().ToUpperInvariant();

        if (status is "ERROR" or "UNAVAILABLE")
        {
            _logger.LogWarning("AI consult status={Status} reason={Reason}", status, body.Reason);
            return new ConsultationResponse(
                false, status, intent, body.ExecutedAgents ?? [],
                null, null, null,
                body.Reason ?? "Tính năng tư vấn AI tạm thời không khả dụng, vui lòng thử lại sau.",
                body.Disclaimer ?? ConsultationResponse.DefaultDisclaimer,
                body.RequestId?.ToString());
        }

        var serviceById = services.ToDictionary(item => item.ServiceId);
        var partById = parts.ToDictionary(item => item.PartId);
        var slotById = slots.ToDictionary(item => item.TimeSlotId.ToString());

        var diagnosis = MapDiagnosis(body.Diagnosis, serviceById, partById);
        var quotation = MapQuotation(body.Quotation, serviceById, partById);
        var appointment = MapAppointment(
            body.Appointment, slotById, request, slotDate);

        if (status == "SUCCESS")
        {
            var hasDiagnosis = diagnosis is not null &&
                (diagnosis.SuggestedServices.Count > 0 || diagnosis.SuggestedParts.Count > 0);
            var hasQuotation = quotation is not null && quotation.Items.Count > 0;
            var hasAppointment = appointment is not null && appointment.SuggestedSlots.Count > 0;

            if (!hasDiagnosis && !hasQuotation && !hasAppointment)
            {
                status = "NO_MATCH";
            }
        }

        var message = body.Reason;
        if (string.IsNullOrWhiteSpace(message))
        {
            message = status switch
            {
                "NO_MATCH" => "Chưa tìm thấy dịch vụ hoặc khung giờ phù hợp. Vui lòng thử lại với mô tả rõ hơn.",
                "OUT_OF_SCOPE" => "Yêu cầu nằm ngoài phạm vi tư vấn sửa chữa thiết bị.",
                _ => "Đã xử lý yêu cầu."
            };
        }

        return new ConsultationResponse(
            true,
            status,
            intent,
            body.ExecutedAgents ?? [],
            diagnosis,
            quotation,
            appointment,
            message,
            body.Disclaimer ?? ConsultationResponse.DefaultDisclaimer,
            body.RequestId?.ToString());
    }

    private static ConsultationDiagnosis? MapDiagnosis(
        AiDiagnosisDto? source,
        Dictionary<string, CatalogServiceItem> serviceById,
        Dictionary<string, CatalogPartItem> partById)
    {
        if (source is null)
        {
            return null;
        }

        var causes = (source.PossibleCauses ?? [])
            .Where(cause => !string.IsNullOrWhiteSpace(cause.Name))
            .Take(3)
            .Select(cause => new ConsultationPossibleCause(
                cause.Name!.Trim(), NormalizeConfidence(cause.Confidence)))
            .ToList();

        var suggestedServices = (source.SuggestedServices ?? [])
            .Where(item => item.ServiceId is not null && serviceById.ContainsKey(item.ServiceId))
            .Take(3)
            .Select(item => new ConsultationServiceItem(
                item.ServiceId!, serviceById[item.ServiceId!].Name,
                NormalizeConfidence(item.Confidence)))
            .ToList();

        var suggestedParts = (source.SuggestedParts ?? [])
            .Where(item => item.PartId is not null && partById.TryGetValue(item.PartId, out var part) && part.InStock)
            .Take(2)
            .Select(item => new ConsultationPartItem(
                item.PartId!, partById[item.PartId!].Name,
                NormalizeConfidence(item.Confidence)))
            .ToList();

        return new ConsultationDiagnosis(
            causes,
            suggestedServices,
            suggestedParts,
            (source.MissingInformation ?? []).Take(5).ToList(),
            source.Explanation ?? string.Empty);
    }

    private static ConsultationQuotation? MapQuotation(
        AiQuotationDto? source,
        Dictionary<string, CatalogServiceItem> serviceById,
        Dictionary<string, CatalogPartItem> partById)
    {
        if (source is null)
        {
            return null;
        }

        var items = new List<ConsultationQuotationItem>();
        foreach (var item in source.Items ?? [])
        {
            if (item.ItemId is null)
            {
                continue;
            }

            decimal price;
            string name;
            string itemType;

            if (serviceById.TryGetValue(item.ItemId, out var service))
            {
                price = service.BasePrice;
                name = service.Name;
                itemType = "SERVICE";
            }
            else if (partById.TryGetValue(item.ItemId, out var part))
            {
                if (!part.InStock)
                {
                    continue;
                }
                price = part.UnitPrice;
                name = part.Name;
                itemType = "PART";
            }
            else
            {
                continue; // ID lạ — loại bỏ
            }

            items.Add(new ConsultationQuotationItem(
                item.ItemId, itemType, name, price, price));
        }

        var total = items.Sum(item => item.LineTotal);
        return new ConsultationQuotation(
            items,
            total,
            source.Currency ?? "VND",
            source.Assumptions ?? [],
            source.Explanation ?? string.Empty);
    }

    private static ConsultationAppointment? MapAppointment(
        AiAppointmentDto? source,
        Dictionary<string, SlotContext> slotById,
        ConsultationRequest request,
        DateOnly slotDate)
    {
        if (source is null)
        {
            return null;
        }

        var suggested = new List<ConsultationSlot>();
        foreach (var slot in source.SuggestedSlots ?? [])
        {
            if (slot.TimeSlotId is null || !slotById.TryGetValue(slot.TimeSlotId, out var context))
            {
                continue; // slot lạ
            }
            if (context.Available <= 0)
            {
                continue; // hết chỗ
            }
            suggested.Add(new ConsultationSlot(
                context.TimeSlotId.ToString(),
                context.SlotStart.ToString("HH:mm"),
                context.SlotEnd.ToString("HH:mm"),
                context.Date,
                context.Available));
        }

        var draft = source.BookingDraft;
        string? slotId = draft?.TimeSlotId;
        if (slotId is not null &&
            (!slotById.TryGetValue(slotId, out var chosen) || chosen.Available <= 0))
        {
            slotId = null;
        }

        var bookingDraft = new ConsultationBookingDraft(
            draft?.FullName ?? request.FullName,
            draft?.Phone ?? request.Phone,
            slotId is not null && slotById.TryGetValue(slotId, out var draftSlot)
                ? draftSlot.Date
                : null,
            slotId,
            request.DeviceType,
            request.Brand,
            request.Model,
            draft?.IssueDescription ?? request.IssueDescription);

        var missing = (source.MissingInformation ?? []).ToList();
        if (slotId is null && !missing.Contains("timeSlotId"))
        {
            missing.Add("timeSlotId");
        }
        if (string.IsNullOrWhiteSpace(bookingDraft.Phone) && !missing.Contains("phone"))
        {
            missing.Add("phone");
        }
        if (string.IsNullOrWhiteSpace(bookingDraft.FullName) && !missing.Contains("fullName"))
        {
            missing.Add("fullName");
        }

        return new ConsultationAppointment(
            suggested,
            bookingDraft,
            missing,
            true,
            source.Explanation ?? string.Empty);
    }

    private static string NormalizeConfidence(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        return normalized is "HIGH" or "MEDIUM" or "LOW" ? normalized : "MEDIUM";
    }

    private static ConsultationResponse NoMatch(string message) =>
        new(true, "NO_MATCH", "UNKNOWN", [], null, null, null,
            message, ConsultationResponse.DefaultDisclaimer);

    private static ConsultationResponse Unavailable() =>
        ConsultationResponse.Unavailable(
            "Tính năng tư vấn AI tạm thời không khả dụng, vui lòng thử lại sau.");

    private sealed record SlotContext(
        Guid TimeSlotId,
        DateOnly Date,
        TimeOnly SlotStart,
        TimeOnly SlotEnd,
        int MaxCapacity,
        int Booked,
        int Available);

    // ── DTO nội bộ khớp JSON camelCase của FastAPI ────────────────────────────

    private sealed record AiConsultResponse(
        [property: JsonPropertyName("requestId")] Guid? RequestId,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("intent")] string? Intent,
        [property: JsonPropertyName("executedAgents")] List<string>? ExecutedAgents,
        [property: JsonPropertyName("diagnosis")] AiDiagnosisDto? Diagnosis,
        [property: JsonPropertyName("quotation")] AiQuotationDto? Quotation,
        [property: JsonPropertyName("appointment")] AiAppointmentDto? Appointment,
        [property: JsonPropertyName("reason")] string? Reason,
        [property: JsonPropertyName("disclaimer")] string? Disclaimer);

    private sealed record AiDiagnosisDto(
        [property: JsonPropertyName("possibleCauses")] List<AiCauseDto>? PossibleCauses,
        [property: JsonPropertyName("suggestedServices")] List<AiServiceDto>? SuggestedServices,
        [property: JsonPropertyName("suggestedParts")] List<AiPartDto>? SuggestedParts,
        [property: JsonPropertyName("missingInformation")] List<string>? MissingInformation,
        [property: JsonPropertyName("explanation")] string? Explanation);

    private sealed record AiCauseDto(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("confidence")] string? Confidence);

    private sealed record AiServiceDto(
        [property: JsonPropertyName("serviceId")] string? ServiceId,
        [property: JsonPropertyName("confidence")] string? Confidence);

    private sealed record AiPartDto(
        [property: JsonPropertyName("partId")] string? PartId,
        [property: JsonPropertyName("confidence")] string? Confidence);

    private sealed record AiQuotationDto(
        [property: JsonPropertyName("items")] List<AiQuotationItemDto>? Items,
        [property: JsonPropertyName("currency")] string? Currency,
        [property: JsonPropertyName("assumptions")] List<string>? Assumptions,
        [property: JsonPropertyName("explanation")] string? Explanation);

    private sealed record AiQuotationItemDto(
        [property: JsonPropertyName("itemId")] string? ItemId);

    private sealed record AiAppointmentDto(
        [property: JsonPropertyName("suggestedSlots")] List<AiSlotDto>? SuggestedSlots,
        [property: JsonPropertyName("bookingDraft")] AiBookingDraftDto? BookingDraft,
        [property: JsonPropertyName("missingInformation")] List<string>? MissingInformation,
        [property: JsonPropertyName("explanation")] string? Explanation);

    private sealed record AiSlotDto(
        [property: JsonPropertyName("timeSlotId")] string? TimeSlotId);

    private sealed record AiBookingDraftDto(
        [property: JsonPropertyName("fullName")] string? FullName,
        [property: JsonPropertyName("phone")] string? Phone,
        [property: JsonPropertyName("timeSlotId")] string? TimeSlotId,
        [property: JsonPropertyName("issueDescription")] string? IssueDescription);
}
