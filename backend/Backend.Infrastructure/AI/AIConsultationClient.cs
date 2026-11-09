using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Extensions.Logging;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.AIAdvisory.DTOs;

namespace RepairShop.Infrastructure.AI;

/// <summary>
/// Task 4.1 — Client gọi MAS (`POST /v1/agents/execute`) thay cho one-shot advisory.
///
/// Trách nhiệm:
/// 1. Dựng context THẬT từ PostgreSQL (services/parts/slots) — AI không truy cập DB.
/// 2. Gọi FastAPI bằng `X-Internal-Api-Key`, timeout cấu hình riêng, bảo vệ bằng Circuit Breaker + giới hạn đồng thời.
/// 3. Hậu kiểm: requestId khớp, ID phải thuộc context, giá lấy từ catalog, slot phải còn chỗ, lọc văn bản AI.
/// 4. Không throw: mọi lỗi AI => `AiAvailable=false` + thông điệp thân thiện (fallback).
/// </summary>
public class AIConsultationClient : IAIConsultationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const int MaxSlotDaysAhead = 7;
    private const int MaxSlotsInContext = 60;
    private const int MaxServiceDescriptionLength = 1000;
    private const int MaxCauseNameLength = 200;
    private const int MaxExplanationLength = 600;

    // Giới hạn tối đa 2 request đồng thời tới Ollama local để tránh hàng đợi 60s.
    private static readonly SemaphoreSlim ConsultConcurrencyGate = new(2, 2);

    private static readonly Regex PriceLeakPattern = new(
        @"\b\d[\d.,]*\s*(đ|₫|vnd|vnđ|nghìn|ngàn|triệu|k\b)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex CertaintyWordPattern = new(
        @"\b(chắc chắn|100%|cam kết|bắt buộc phải thay|chính thức)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly HttpClient _httpClient;
    private readonly IServiceCatalogQueryService _serviceCatalog;
    private readonly ITimeSlotConfigRepository _slotRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IBusinessClock _clock;
    private readonly IAICircuitBreaker? _circuitBreaker;
    private readonly ILogger<AIConsultationClient> _logger;

    public AIConsultationClient(
        HttpClient httpClient,
        IServiceCatalogQueryService serviceCatalog,
        ITimeSlotConfigRepository slotRepository,
        IAppointmentRepository appointmentRepository,
        IBusinessClock clock,
        ILogger<AIConsultationClient> logger,
        IAICircuitBreaker? circuitBreaker = null)
    {
        _httpClient = httpClient;
        _serviceCatalog = serviceCatalog;
        _slotRepository = slotRepository;
        _appointmentRepository = appointmentRepository;
        _clock = clock;
        _logger = logger;
        _circuitBreaker = circuitBreaker;
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

        if (_circuitBreaker is not null && !_circuitBreaker.CanExecute())
        {
            _logger.LogWarning("AI Circuit Breaker đang OPEN — bỏ qua gọi AI consult.");
            return Unavailable();
        }

        var acquired = await ConsultConcurrencyGate.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        if (!acquired)
        {
            _logger.LogWarning("Hệ thống tư vấn AI đang xử lý tối đa số yêu cầu đồng thời.");
            return Unavailable();
        }

        try
        {
            var services = await _serviceCatalog.GetAvailableServicesAsync(request.DeviceType);
            var parts = await _serviceCatalog.GetAvailablePartsAsync(request.DeviceType);
            var (slotDate, slots) = await BuildSlotsAsync(request.RequestedDate, today, cancellationToken);

            var requestId = Guid.NewGuid();
            var actorType = string.Equals(request.ActorType, "CUSTOMER", StringComparison.OrdinalIgnoreCase)
                ? "CUSTOMER"
                : "GUEST";

            var payload = new
            {
                requestId,
                actorType,
                referenceDate = today.ToString("yyyy-MM-dd"),
                message = request.IssueDescription.Trim(),
                device = new
                {
                    deviceType = request.DeviceType.Trim().ToLowerInvariant(),
                    brand = request.Brand.Trim(),
                    model = request.Model.Trim()
                },
                issueDescription = request.IssueDescription.Trim(),
                context = new
                {
                    availableServices = services.Select(service => new
                    {
                        serviceId = service.ServiceId,
                        name = Truncate(service.Name, 150) ?? service.ServiceId,
                        description = Truncate(service.Description, MaxServiceDescriptionLength),
                        deviceType = string.IsNullOrWhiteSpace(service.DeviceType)
                            ? request.DeviceType.Trim().ToLowerInvariant()
                            : service.DeviceType.Trim().ToLowerInvariant(),
                        basePrice = service.BasePrice
                    }),
                    availableParts = parts.Select(part => new
                    {
                        partId = part.PartId,
                        name = Truncate(part.Name, 150) ?? part.PartId,
                        unitPrice = part.UnitPrice,
                        compatibleDeviceType = part.CompatibleDeviceType?.Trim().ToLowerInvariant(),
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
                _circuitBreaker?.RecordFailure();
                return Unavailable();
            }

            var body = await httpResponse.Content.ReadFromJsonAsync<AiConsultResponse>(
                JsonOptions, cancellationToken);

            if (body is null)
            {
                _logger.LogError("AI consult trả body rỗng.");
                _circuitBreaker?.RecordFailure();
                return Unavailable();
            }

            // Luật hậu kiểm 1 (MAS §5.3): requestId phản hồi phải khớp requestId đã gửi (nếu AI có trả).
            if (body.RequestId is { } respId && respId != Guid.Empty && respId != requestId)
            {
                _logger.LogWarning(
                    "AI consult trả requestId {ResponseRequestId} không khớp {ExpectedRequestId} — từ chối.",
                    respId,
                    requestId);
                _circuitBreaker?.RecordFailure();
                return Unavailable();
            }

            _circuitBreaker?.RecordSuccess();
            return MapResponse(request, body, services, parts, slots, slotDate, requestId);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "AI consult timeout.");
            _circuitBreaker?.RecordFailure();
            return Unavailable();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Không kết nối được AI Service cho consult.");
            _circuitBreaker?.RecordFailure();
            return Unavailable();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "AI consult trả JSON không hợp lệ.");
            _circuitBreaker?.RecordFailure();
            return Unavailable();
        }
        finally
        {
            ConsultConcurrencyGate.Release();
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

        // Gom truy vấn đếm lịch hẹn theo lô nếu repository hỗ trợ, fallback về CountBookedAsync cho từng slot.
        var batchBooked = await _appointmentRepository.CountBookedBatchAsync(dates, cancellationToken);
        var allSlots = new List<SlotContext>();
        DateOnly? firstDateWithAvailable = null;

        foreach (var date in dates)
        {
            foreach (var config in configurations
                         .Where(config => config.IsActive && config.AppliesTo(date))
                         .OrderBy(config => config.SlotStart))
            {
                if (allSlots.Count >= MaxSlotsInContext)
                {
                    break;
                }

                int booked;
                if (batchBooked.Count > 0)
                {
                    booked = batchBooked.GetValueOrDefault((date, config.Id), 0);
                }
                else
                {
                    booked = await _appointmentRepository.CountBookedAsync(date, config.Id);
                }

                var available = Math.Max(0, config.MaxCapacity - booked);
                allSlots.Add(new SlotContext(
                    config.Id, date, config.SlotStart, config.SlotEnd,
                    config.MaxCapacity, booked, available));

                if (available > 0 && firstDateWithAvailable is null)
                {
                    firstDateWithAvailable = date;
                }
            }

            if (requestedDate is not null || allSlots.Count >= MaxSlotsInContext)
            {
                break;
            }
        }

        var defaultDate = requestedDate ?? firstDateWithAvailable ?? today.AddDays(1);
        return (defaultDate, allSlots);
    }

    private ConsultationResponse MapResponse(
        ConsultationRequest request,
        AiConsultResponse body,
        List<CatalogServiceItem> services,
        List<CatalogPartItem> parts,
        List<SlotContext> slots,
        DateOnly slotDate,
        Guid expectedRequestId)
    {
        var status = (body.Status ?? "ERROR").Trim().ToUpperInvariant();
        var intent = (body.Intent ?? "UNKNOWN").Trim().ToUpperInvariant();
        var resolvedRequestId = (body.RequestId ?? expectedRequestId).ToString();

        if (status is "ERROR" or "UNAVAILABLE")
        {
            _logger.LogWarning("AI consult status={Status} reason={Reason}", status, body.Reason);
            return new ConsultationResponse(
                false, status, intent, body.ExecutedAgents ?? [],
                null, null, null,
                body.Reason ?? "Tính năng tư vấn AI tạm thời không khả dụng, vui lòng thử lại sau.",
                body.Disclaimer ?? ConsultationResponse.DefaultDisclaimer,
                resolvedRequestId);
        }

        var serviceById = services.ToDictionary(item => item.ServiceId);
        var partById = parts.ToDictionary(item => item.PartId);
        // Nhiều ngày có thể dùng cùng TimeSlotId cấu hình — tạo dictionary theo key (TimeSlotId, Date) và fallback theo TimeSlotId.
        var slotByIdAndDate = slots
            .GroupBy(item => (Id: item.TimeSlotId.ToString(), Date: item.Date.ToString("yyyy-MM-dd")))
            .ToDictionary(g => g.Key, g => g.First());
        var slotById = slots
            .GroupBy(item => item.TimeSlotId.ToString())
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.Available).First());

        var diagnosis = MapDiagnosis(body.Diagnosis, serviceById, partById);
        var quotation = MapQuotation(body.Quotation, serviceById, partById);
        var appointment = MapAppointment(
            body.Appointment, slotByIdAndDate, slotById, request, slotDate);

        if (status == "SUCCESS")
        {
            var hasDiagnosis = diagnosis is not null &&
                (diagnosis.SuggestedServices.Count > 0 || diagnosis.SuggestedParts.Count > 0);
            var hasQuotation = quotation is not null && quotation.Items.Count > 0;
            var hasAppointment = appointment is not null && appointment.SuggestedSlots.Count > 0;

            if (!hasDiagnosis && !hasQuotation && !hasAppointment &&
                intent is not ("UNKNOWN" or "TRACKING"))
            {
                status = "NO_MATCH";
            }
        }

        var message = SanitizeReasonText(body.Reason);
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
            resolvedRequestId);
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
                SanitizeCauseName(cause.Name!), NormalizeConfidence(cause.Confidence)))
            .Where(cause => !string.IsNullOrWhiteSpace(cause.Name))
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

        var missing = (source.MissingInformation ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Take(5)
            .Select(item => Truncate(item.Trim(), 120)!)
            .ToList();

        var explanation = SanitizeDiagnosisExplanation(source.Explanation, causes);

        return new ConsultationDiagnosis(
            causes,
            suggestedServices,
            suggestedParts,
            missing,
            explanation);
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
            (source.Assumptions ?? []).Take(5).Select(a => Truncate(a.Trim(), 200)!).ToList(),
            Truncate(source.Explanation?.Trim(), MaxExplanationLength) ?? string.Empty);
    }

    private static ConsultationAppointment? MapAppointment(
        AiAppointmentDto? source,
        Dictionary<(string Id, string Date), SlotContext> slotByIdAndDate,
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
            if (slot.TimeSlotId is null)
            {
                continue;
            }

            SlotContext? context = null;
            if (!string.IsNullOrWhiteSpace(slot.AppointmentDate) &&
                slotByIdAndDate.TryGetValue((slot.TimeSlotId, slot.AppointmentDate!), out var datedSlot))
            {
                context = datedSlot;
            }
            else if (slotById.TryGetValue(slot.TimeSlotId, out var fallbackSlot))
            {
                context = fallbackSlot;
            }

            if (context is null || context.Available <= 0)
            {
                continue; // slot lạ hoặc hết chỗ
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
        SlotContext? chosenContext = null;
        if (slotId is not null)
        {
            if (!string.IsNullOrWhiteSpace(draft?.AppointmentDate) &&
                slotByIdAndDate.TryGetValue((slotId, draft!.AppointmentDate!), out var datedChosen))
            {
                chosenContext = datedChosen;
            }
            else if (slotById.TryGetValue(slotId, out var fallbackChosen))
            {
                chosenContext = fallbackChosen;
            }

            if (chosenContext is null || chosenContext.Available <= 0)
            {
                slotId = null;
                chosenContext = null;
            }
        }

        DateOnly? resolvedDraftDate = chosenContext?.Date;
        if (resolvedDraftDate is null &&
            !string.IsNullOrWhiteSpace(draft?.AppointmentDate) &&
            DateOnly.TryParse(draft!.AppointmentDate, out var parsedDraftDate))
        {
            resolvedDraftDate = parsedDraftDate;
        }

        var bookingDraft = new ConsultationBookingDraft(
            Truncate(draft?.FullName ?? request.FullName, 100),
            Truncate(draft?.Phone ?? request.Phone, 20),
            resolvedDraftDate,
            slotId,
            request.DeviceType,
            request.Brand,
            request.Model,
            Truncate(draft?.IssueDescription ?? request.IssueDescription, 500));

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
            Truncate(source.Explanation?.Trim(), MaxExplanationLength) ?? string.Empty);
    }

    private static string SanitizeCauseName(string raw)
    {
        var cleaned = raw.Trim();
        if (PriceLeakPattern.IsMatch(cleaned))
        {
            cleaned = PriceLeakPattern.Replace(cleaned, "").Trim();
        }
        if (CertaintyWordPattern.IsMatch(cleaned))
        {
            cleaned = CertaintyWordPattern.Replace(cleaned, "có khả năng").Trim();
        }
        return Truncate(cleaned, MaxCauseNameLength) ?? string.Empty;
    }

    private static string SanitizeDiagnosisExplanation(
        string? raw,
        IReadOnlyCollection<ConsultationPossibleCause> causes)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var cleaned = raw.Trim();
        // Nếu LLM chèn giá tự bịa hoặc từ khẳng định tuyệt đối vào phần giải thích chẩn đoán => thay thế an toàn.
        if (PriceLeakPattern.IsMatch(cleaned) || CertaintyWordPattern.IsMatch(cleaned))
        {
            return causes.Count > 0
                ? $"Triệu chứng mô tả có thể liên quan đến: {string.Join(", ", causes.Select(c => c.Name))}. Kỹ thuật viên cần kiểm tra trực tiếp để kết luận chính xác."
                : "Đây là nhận định sơ bộ từ mô tả triệu chứng. Kỹ thuật viên sẽ kiểm tra thực tế tại cửa hàng.";
        }

        return Truncate(cleaned, MaxExplanationLength) ?? string.Empty;
    }

    private static string? SanitizeReasonText(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }
        var cleaned = CertaintyWordPattern.Replace(raw.Trim(), "có khả năng");
        return Truncate(cleaned, 500);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (value is null)
        {
            return null;
        }
        return value.Length <= maxLength ? value : value[..maxLength];
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
        [property: JsonPropertyName("timeSlotId")] string? TimeSlotId,
        [property: JsonPropertyName("appointmentDate")] string? AppointmentDate);

    private sealed record AiBookingDraftDto(
        [property: JsonPropertyName("fullName")] string? FullName,
        [property: JsonPropertyName("phone")] string? Phone,
        [property: JsonPropertyName("appointmentDate")] string? AppointmentDate,
        [property: JsonPropertyName("timeSlotId")] string? TimeSlotId,
        [property: JsonPropertyName("issueDescription")] string? IssueDescription);
}
