using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Extensions.Logging;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.AIAdvisory.DTOs;
using RepairShop.Domain.Common;
using RepairShop.Domain.Modules.Tickets;
using RepairShop.Domain.Modules.Warranty;
using RepairShop.Domain.Modules.Warranty.Enums;

namespace RepairShop.Infrastructure.AI;

/// <summary>
/// Task 4.1 — Client gọi MAS (`POST /v1/agents/execute`) thay cho one-shot advisory.
///
/// Trách nhiệm:
/// 1. Dựng context THẬT từ PostgreSQL (services/parts/slots/warranty/tracking) — AI không truy cập DB.
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
    private const int MaxLookupItems = 20;

    // Giới hạn tối đa 2 request đồng thời tới Ollama local để tránh hàng đợi 60s.
    private static readonly SemaphoreSlim ConsultConcurrencyGate = new(2, 2);

    private static readonly Regex PriceLeakPattern = new(
        @"\b\d[\d.,]*\s*(đ|₫|vnd|vnđ|nghìn|ngàn|triệu|k\b)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex CertaintyWordPattern = new(
        @"\b(chắc chắn|100%|cam kết|bắt buộc phải thay|chính thức)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TicketCodePattern = new(
        @"\b(?:RS|RT|TK|HD|SC)(?:[-_][A-Z0-9]{2,15}(?:[-_][A-Z0-9]{1,15})*|(?=[A-Z0-9]*\d)[A-Z0-9]{3,20})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex WarrantyCodePattern = new(
        @"\b(?:BH|WR|WAR)(?:[-_][A-Z0-9]{2,15}(?:[-_][A-Z0-9]{1,15})*|(?=[A-Z0-9]*\d)[A-Z0-9]{3,20})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Dictionary<string, string> StatusLabels = new()
    {
        [RepairStatusCodes.CheckedIn] = "Đã tiếp nhận",
        [RepairStatusCodes.Assigned] = "Đã phân công kỹ thuật viên",
        [RepairStatusCodes.Diagnosing] = "Đang kiểm tra",
        [RepairStatusCodes.WaitingApproval] = "Chờ khách xác nhận báo giá",
        [RepairStatusCodes.OnHold] = "Tạm hoãn",
        [RepairStatusCodes.WaitingParts] = "Chờ linh kiện",
        [RepairStatusCodes.InRepair] = "Đang sửa chữa",
        [RepairStatusCodes.QaTesting] = "Đang kiểm thử",
        [RepairStatusCodes.ReadyForPickup] = "Sẵn sàng bàn giao",
        [RepairStatusCodes.Delivered] = "Đã bàn giao",
        [RepairStatusCodes.ClosedRejected] = "Đã đóng (từ chối báo giá)"
    };

    private readonly HttpClient _httpClient;
    private readonly IServiceCatalogQueryService _serviceCatalog;
    private readonly ITimeSlotConfigRepository _slotRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IBusinessClock _clock;
    private readonly IAICircuitBreaker? _circuitBreaker;
    private readonly ILogger<AIConsultationClient> _logger;
    private readonly IWarrantyPackageRepository? _warrantyPackageRepository;
    private readonly IWarrantyRepository? _warrantyRepository;
    private readonly IRepairTicketRepository? _repairTicketRepository;
    private readonly ICustomerRepository? _customerRepository;

    public AIConsultationClient(
        HttpClient httpClient,
        IServiceCatalogQueryService serviceCatalog,
        ITimeSlotConfigRepository slotRepository,
        IAppointmentRepository appointmentRepository,
        IBusinessClock clock,
        ILogger<AIConsultationClient> logger,
        IAICircuitBreaker? circuitBreaker = null,
        IWarrantyPackageRepository? warrantyPackageRepository = null,
        IWarrantyRepository? warrantyRepository = null,
        IRepairTicketRepository? repairTicketRepository = null,
        ICustomerRepository? customerRepository = null)
    {
        _httpClient = httpClient;
        _serviceCatalog = serviceCatalog;
        _slotRepository = slotRepository;
        _appointmentRepository = appointmentRepository;
        _clock = clock;
        _logger = logger;
        _circuitBreaker = circuitBreaker;
        _warrantyPackageRepository = warrantyPackageRepository;
        _warrantyRepository = warrantyRepository;
        _repairTicketRepository = repairTicketRepository;
        _customerRepository = customerRepository;
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
            var explicitTicketCode = ExtractCode(request.TicketCode, request.IssueDescription, TicketCodePattern);
            var explicitWarrantyCode = ExtractCode(request.WarrantyCode, request.IssueDescription, WarrantyCodePattern);

            var customerId = await ResolveCustomerIdAsync(actorType, request.UserId);
            var warrantyPolicies = await BuildWarrantyPoliciesAsync(request.AllowWarranty);
            var warrantyLookups = await BuildWarrantyLookupsAsync(
                request.AllowWarranty, actorType, customerId, explicitWarrantyCode, today, cancellationToken);
            var trackingLookups = await BuildTrackingLookupsAsync(
                request.AllowTracking, actorType, customerId, explicitTicketCode);

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
                        inStock = part.InStock,
                        category = Truncate(part.Category, 100),
                        quantityOnHand = part.QuantityOnHand
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
                    }),
                    warrantyPolicies = warrantyPolicies.Select(policy => new
                    {
                        packageId = policy.PackageId,
                        name = policy.Name,
                        durationMonths = policy.DurationMonths,
                        terms = policy.Terms,
                        description = policy.Description,
                        isActive = policy.IsActive
                    }),
                    warrantyLookups = warrantyLookups.Select(lookup => new
                    {
                        warrantyCode = lookup.WarrantyCode,
                        ticketCode = lookup.TicketCode,
                        deviceLabel = lookup.DeviceLabel,
                        startDate = lookup.StartDate,
                        endDate = lookup.EndDate,
                        status = lookup.Status,
                        isExpired = lookup.IsExpired,
                        packageName = lookup.PackageName,
                        terms = lookup.Terms
                    }),
                    trackingLookups = trackingLookups.Select(lookup => new
                    {
                        ticketCode = lookup.TicketCode,
                        deviceLabel = lookup.DeviceLabel,
                        status = lookup.Status,
                        statusLabel = lookup.StatusLabel,
                        statusHistory = lookup.StatusHistory.Select(item => new
                        {
                            status = item.Status,
                            statusLabel = item.StatusLabel,
                            changedAt = item.ChangedAt
                        }),
                        estimatedCompletion = lookup.EstimatedCompletion
                    })
                },
                options = new
                {
                    allowDiagnosis = true,
                    allowQuotation = request.AllowQuotation,
                    allowAppointment = request.AllowAppointment,
                    allowWarranty = request.AllowWarranty,
                    allowTracking = request.AllowTracking
                },
                customerInfo = new
                {
                    fullName = request.FullName,
                    phone = request.Phone
                },
                ticketCode = Truncate(explicitTicketCode, 50),
                warrantyCode = Truncate(explicitWarrantyCode, 50)
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
            return MapResponse(
                request,
                body,
                services,
                parts,
                slots,
                slotDate,
                warrantyPolicies,
                warrantyLookups,
                trackingLookups,
                requestId);
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
        List<WarrantyPolicyContext> warrantyPolicies,
        List<WarrantyLookupContext> warrantyLookups,
        List<TrackingLookupContext> trackingLookups,
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
        var policyById = warrantyPolicies.ToDictionary(item => item.PackageId, StringComparer.OrdinalIgnoreCase);
        var warrantyByCode = warrantyLookups.ToDictionary(item => item.WarrantyCode, StringComparer.OrdinalIgnoreCase);
        var trackingByCode = trackingLookups.ToDictionary(item => item.TicketCode, StringComparer.OrdinalIgnoreCase);

        var diagnosis = MapDiagnosis(body.Diagnosis, serviceById, partById);
        var quotation = MapQuotation(body.Quotation, serviceById, partById);
        var appointment = MapAppointment(
            body.Appointment, slotByIdAndDate, slotById, request, slotDate);
        var warranty = MapWarranty(body.Warranty, policyById, warrantyByCode);
        var tracking = MapTracking(body.Tracking, trackingByCode);

        if (status == "SUCCESS")
        {
            var hasDiagnosis = diagnosis is not null &&
                (diagnosis.SuggestedServices.Count > 0 || diagnosis.SuggestedParts.Count > 0);
            var hasQuotation = quotation is not null && quotation.Items.Count > 0;
            var hasAppointment = appointment is not null && appointment.SuggestedSlots.Count > 0;
            var hasWarranty = warranty is not null &&
                (warranty.Policies.Count > 0 || warranty.Warranties.Count > 0);
            var hasTracking = tracking is not null && tracking.Tickets.Count > 0;
            var aiHadWarrantyOrTracking =
                (body.Warranty?.Policies?.Count > 0) ||
                (body.Warranty?.Warranties?.Count > 0) ||
                (body.Tracking?.Tickets?.Count > 0);

            if (!hasDiagnosis && !hasQuotation && !hasAppointment && !hasWarranty && !hasTracking &&
                (aiHadWarrantyOrTracking || intent is not ("UNKNOWN" or "TRACKING" or "WARRANTY")))
            {
                status = "NO_MATCH";
            }
        }

        var message = SanitizeReasonText(body.Reason);
        if (string.IsNullOrWhiteSpace(message))
        {
            message = status switch
            {
                "NEED_CLARIFICATION" => "Vui lòng cung cấp thêm thông tin để hệ thống tư vấn chính xác hơn.",
                "NO_MATCH" => "Chưa tìm thấy dịch vụ hoặc khung giờ phù hợp. Vui lòng thử lại với mô tả rõ hơn.",
                "OUT_OF_SCOPE" => "Yêu cầu nằm ngoài phạm vi tư vấn sửa chữa thiết bị.",
                _ => "Đã xử lý yêu cầu."
            };
        }

        var missingInfo = (body.MissingInformation ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Take(5)
            .Select(item => Truncate(item.Trim(), 120)!)
            .ToList();

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
            resolvedRequestId,
            Warranty: warranty,
            Tracking: tracking,
            MissingInformation: missingInfo.Count > 0 ? missingInfo : null);
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
                NormalizeConfidence(item.Confidence),
                item.MatchingScore is >= 0.0 and <= 1.0 ? Math.Round(item.MatchingScore.Value, 4) : null))
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

    private static ConsultationWarranty? MapWarranty(
        AiWarrantyDto? source,
        Dictionary<string, WarrantyPolicyContext> policyById,
        Dictionary<string, WarrantyLookupContext> warrantyByCode)
    {
        if (source is null)
        {
            return null;
        }

        var seenPolicies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var policies = new List<ConsultationWarrantyPolicy>();
        foreach (var item in source.Policies ?? [])
        {
            if (string.IsNullOrWhiteSpace(item.PackageId) ||
                !policyById.TryGetValue(item.PackageId.Trim(), out var contextPolicy) ||
                !seenPolicies.Add(contextPolicy.PackageId))
            {
                continue;
            }

            policies.Add(new ConsultationWarrantyPolicy(
                contextPolicy.PackageId,
                contextPolicy.Name,
                contextPolicy.DurationMonths,
                contextPolicy.Terms,
                contextPolicy.Description));
        }

        var seenWarranties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var warranties = new List<ConsultationWarrantyRecord>();
        foreach (var item in source.Warranties ?? [])
        {
            if (string.IsNullOrWhiteSpace(item.WarrantyCode) ||
                !warrantyByCode.TryGetValue(item.WarrantyCode.Trim(), out var contextWarranty) ||
                !seenWarranties.Add(contextWarranty.WarrantyCode))
            {
                continue;
            }

            warranties.Add(new ConsultationWarrantyRecord(
                contextWarranty.WarrantyCode,
                contextWarranty.TicketCode,
                contextWarranty.DeviceLabel,
                contextWarranty.Status,
                contextWarranty.StartDate,
                contextWarranty.EndDate,
                contextWarranty.IsExpired,
                contextWarranty.PackageName,
                contextWarranty.Terms));
        }

        var missing = (source.MissingInformation ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Take(5)
            .Select(item => Truncate(item.Trim(), 120)!)
            .ToList();

        return new ConsultationWarranty(
            policies,
            warranties,
            missing,
            warranties.Count > 0 || source.RequiresVerification,
            SanitizeReasonText(source.Explanation) ?? string.Empty);
    }

    private static ConsultationTracking? MapTracking(
        AiTrackingDto? source,
        Dictionary<string, TrackingLookupContext> trackingByCode)
    {
        if (source is null)
        {
            return null;
        }

        var seenTickets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var tickets = new List<ConsultationTrackingTicket>();
        foreach (var item in source.Tickets ?? [])
        {
            if (string.IsNullOrWhiteSpace(item.TicketCode) ||
                !trackingByCode.TryGetValue(item.TicketCode.Trim(), out var contextTicket) ||
                !seenTickets.Add(contextTicket.TicketCode))
            {
                continue;
            }

            var history = contextTicket.StatusHistory
                .Select(h => new ConsultationTrackingHistoryItem(h.Status, h.StatusLabel, h.ChangedAt))
                .ToList();

            tickets.Add(new ConsultationTrackingTicket(
                contextTicket.TicketCode,
                contextTicket.DeviceLabel,
                contextTicket.Status,
                contextTicket.StatusLabel,
                history,
                contextTicket.EstimatedCompletion));
        }

        var missing = (source.MissingInformation ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Take(5)
            .Select(item => Truncate(item.Trim(), 120)!)
            .ToList();

        return new ConsultationTracking(
            tickets,
            missing,
            SanitizeReasonText(source.Explanation) ?? string.Empty);
    }

    private static string? ExtractCode(string? explicitCode, string? text, Regex pattern)
    {
        if (!string.IsNullOrWhiteSpace(explicitCode))
        {
            return explicitCode.Trim();
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var match = pattern.Match(text);
        return match.Success ? match.Value.Trim().ToUpperInvariant() : null;
    }

    private async Task<Guid?> ResolveCustomerIdAsync(string actorType, Guid? userId)
    {
        if (!string.Equals(actorType, "CUSTOMER", StringComparison.OrdinalIgnoreCase) ||
            userId is null ||
            _customerRepository is null)
        {
            return null;
        }

        var customer = await _customerRepository.GetByUserIdAsync(userId.Value);
        return customer?.Id;
    }

    private async Task<List<WarrantyPolicyContext>> BuildWarrantyPoliciesAsync(bool allowWarranty)
    {
        if (!allowWarranty || _warrantyPackageRepository is null)
        {
            return [];
        }

        var packages = await _warrantyPackageRepository.SearchAsync(true);
        return packages
            .Take(50)
            .Select(p => new WarrantyPolicyContext(
                p.Id.ToString(),
                Truncate(p.Name, 150) ?? "Gói bảo hành",
                Math.Max(1, p.DurationMonths),
                Truncate(string.IsNullOrWhiteSpace(p.Terms)
                    ? "Áp dụng theo điều khoản bảo hành chuẩn của cửa hàng."
                    : p.Terms.Trim(), 1000)!,
                Truncate(p.Description?.Trim(), 500),
                p.IsActive))
            .ToList();
    }

    private async Task<List<WarrantyLookupContext>> BuildWarrantyLookupsAsync(
        bool allowWarranty,
        string actorType,
        Guid? customerId,
        string? explicitWarrantyCode,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        if (!allowWarranty || _warrantyRepository is null)
        {
            return [];
        }

        if (!string.IsNullOrWhiteSpace(explicitWarrantyCode))
        {
            var view = await _warrantyRepository.GetByWarrantyCodeForPublicAsync(
                explicitWarrantyCode, cancellationToken);
            if (view is null && explicitWarrantyCode != explicitWarrantyCode.ToUpperInvariant())
            {
                view = await _warrantyRepository.GetByWarrantyCodeForPublicAsync(
                    explicitWarrantyCode.ToUpperInvariant(), cancellationToken);
            }

            return view is null
                ? []
                : [MapWarrantyView(view.Warranty, view.TicketCode, view.DeviceLabel, view.PackageName, today)];
        }

        // Bảo vệ cô lập GUEST/CUSTOMER: khách vãng lai KHÔNG bao giờ được xem danh sách bảo hành cá nhân.
        if (!string.Equals(actorType, "CUSTOMER", StringComparison.OrdinalIgnoreCase) ||
            customerId is null)
        {
            return [];
        }

        var warranties = await _warrantyRepository.GetByCustomerIdAsync(customerId.Value);
        return warranties
            .Take(MaxLookupItems)
            .Select(v => MapWarrantyView(
                v.Warranty,
                v.TicketCode,
                v.DeviceLabel,
                v.Warranty.WarrantyPackage?.Name,
                today))
            .ToList();
    }

    private async Task<List<TrackingLookupContext>> BuildTrackingLookupsAsync(
        bool allowTracking,
        string actorType,
        Guid? customerId,
        string? explicitTicketCode)
    {
        if (!allowTracking || _repairTicketRepository is null)
        {
            return [];
        }

        if (!string.IsNullOrWhiteSpace(explicitTicketCode))
        {
            var ticket = await _repairTicketRepository.GetByTicketCodeForTrackingAsync(explicitTicketCode);
            if (ticket is null && explicitTicketCode != explicitTicketCode.ToUpperInvariant())
            {
                ticket = await _repairTicketRepository.GetByTicketCodeForTrackingAsync(
                    explicitTicketCode.ToUpperInvariant());
            }

            return ticket is null ? [] : [MapTicketLookup(ticket)];
        }

        // Bảo vệ cô lập GUEST/CUSTOMER: khách vãng lai chỉ được tra cứu khi có mã phiếu cụ thể.
        if (!string.Equals(actorType, "CUSTOMER", StringComparison.OrdinalIgnoreCase) ||
            customerId is null)
        {
            return [];
        }

        var tickets = await _repairTicketRepository.GetByCustomerForHistoryAsync(customerId.Value);
        return tickets
            .Take(MaxLookupItems)
            .Select(MapTicketLookup)
            .ToList();
    }

    private static WarrantyLookupContext MapWarrantyView(
        Domain.Modules.Warranty.Warranty warranty,
        string ticketCode,
        string deviceLabel,
        string? packageName,
        DateOnly today)
    {
        var isExpired = warranty.IsExpired() || warranty.EndDate < today;
        var status = warranty.Status == WarrantyStatus.Voided
            ? "Voided"
            : isExpired
                ? "Expired"
                : "Active";

        return new WarrantyLookupContext(
            Truncate(warranty.WarrantyCode, 50) ?? string.Empty,
            Truncate(ticketCode, 50) ?? string.Empty,
            Truncate(string.IsNullOrWhiteSpace(deviceLabel) ? "Thiết bị" : deviceLabel.Trim(), 200)!,
            warranty.StartDate.ToString("yyyy-MM-dd"),
            warranty.EndDate.ToString("yyyy-MM-dd"),
            status,
            isExpired,
            Truncate(packageName?.Trim(), 150),
            Truncate(warranty.Terms?.Trim(), 500));
    }

    private static TrackingLookupContext MapTicketLookup(RepairTicket ticket)
    {
        var statusCode = string.IsNullOrWhiteSpace(ticket.Status?.Code)
            ? RepairStatusCodes.CheckedIn
            : ticket.Status.Code;
        var statusLabel = StatusLabels.GetValueOrDefault(
            statusCode,
            string.IsNullOrWhiteSpace(ticket.Status?.Name) ? statusCode : ticket.Status.Name);
        var rawDevice = ticket.Device is null
            ? "Thiết bị"
            : $"{ticket.Device.Brand} {ticket.Device.Model}".Trim();
        var deviceLabel = Truncate(string.IsNullOrWhiteSpace(rawDevice) ? "Thiết bị" : rawDevice, 200)!;

        // Chủ động KHÔNG map SerialNumber, ChangedByUser hay internal Note.
        var history = (ticket.StatusHistories ?? [])
            .OrderBy(h => h.ChangedAt)
            .Where(h => !string.IsNullOrWhiteSpace(h.Status?.Code))
            .Select(h => new TrackingHistoryContext(
                Truncate(h.Status.Code, 50)!,
                Truncate(StatusLabels.GetValueOrDefault(h.Status.Code, h.Status.Name), 100)!,
                h.ChangedAt.ToString("O")))
            .ToList();

        return new TrackingLookupContext(
            Truncate(ticket.TicketCode, 50) ?? string.Empty,
            deviceLabel,
            Truncate(statusCode, 50)!,
            Truncate(statusLabel, 100)!,
            history,
            null);
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

    private sealed record WarrantyPolicyContext(
        string PackageId,
        string Name,
        int DurationMonths,
        string Terms,
        string? Description,
        bool IsActive);

    private sealed record WarrantyLookupContext(
        string WarrantyCode,
        string TicketCode,
        string DeviceLabel,
        string StartDate,
        string EndDate,
        string Status,
        bool IsExpired,
        string? PackageName,
        string? Terms);

    private sealed record TrackingHistoryContext(
        string Status,
        string StatusLabel,
        string ChangedAt);

    private sealed record TrackingLookupContext(
        string TicketCode,
        string DeviceLabel,
        string Status,
        string StatusLabel,
        List<TrackingHistoryContext> StatusHistory,
        string? EstimatedCompletion);

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
        [property: JsonPropertyName("disclaimer")] string? Disclaimer,
        [property: JsonPropertyName("warranty")] AiWarrantyDto? Warranty = null,
        [property: JsonPropertyName("tracking")] AiTrackingDto? Tracking = null,
        [property: JsonPropertyName("missingInformation")] List<string>? MissingInformation = null);

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
        [property: JsonPropertyName("confidence")] string? Confidence,
        [property: JsonPropertyName("matchingScore")] double? MatchingScore = null);

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

    private sealed record AiWarrantyDto(
        [property: JsonPropertyName("policies")] List<AiWarrantyPolicyDto>? Policies,
        [property: JsonPropertyName("warranties")] List<AiWarrantyRecordDto>? Warranties,
        [property: JsonPropertyName("missingInformation")] List<string>? MissingInformation,
        [property: JsonPropertyName("requiresVerification")] bool RequiresVerification,
        [property: JsonPropertyName("explanation")] string? Explanation);

    private sealed record AiWarrantyPolicyDto(
        [property: JsonPropertyName("packageId")] string? PackageId,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("durationMonths")] int DurationMonths,
        [property: JsonPropertyName("terms")] string? Terms,
        [property: JsonPropertyName("description")] string? Description);

    private sealed record AiWarrantyRecordDto(
        [property: JsonPropertyName("warrantyCode")] string? WarrantyCode,
        [property: JsonPropertyName("ticketCode")] string? TicketCode,
        [property: JsonPropertyName("deviceLabel")] string? DeviceLabel,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("startDate")] string? StartDate,
        [property: JsonPropertyName("endDate")] string? EndDate,
        [property: JsonPropertyName("isExpired")] bool IsExpired,
        [property: JsonPropertyName("packageName")] string? PackageName,
        [property: JsonPropertyName("terms")] string? Terms);

    private sealed record AiTrackingDto(
        [property: JsonPropertyName("tickets")] List<AiTrackingTicketDto>? Tickets,
        [property: JsonPropertyName("missingInformation")] List<string>? MissingInformation,
        [property: JsonPropertyName("explanation")] string? Explanation);

    private sealed record AiTrackingTicketDto(
        [property: JsonPropertyName("ticketCode")] string? TicketCode,
        [property: JsonPropertyName("deviceLabel")] string? DeviceLabel,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("statusLabel")] string? StatusLabel,
        [property: JsonPropertyName("statusHistory")] List<AiTrackingHistoryItemDto>? StatusHistory,
        [property: JsonPropertyName("estimatedCompletion")] string? EstimatedCompletion);

    private sealed record AiTrackingHistoryItemDto(
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("statusLabel")] string? StatusLabel,
        [property: JsonPropertyName("changedAt")] string? ChangedAt);
}
