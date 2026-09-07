using System.Net;
using System.Net.Http.Json;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.AIAdvisory.DTOs;
using Microsoft.Extensions.Logging;

namespace RepairShop.Infrastructure.AI;

public class AIServiceClient : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly IServiceCatalogQueryService _serviceCatalog;
    private readonly IPartRepository _partRepository;
    private readonly ILogger<AIServiceClient> _logger;
    private readonly IAICircuitBreaker _circuitBreaker;

    public AIServiceClient(
        HttpClient httpClient,
        IServiceCatalogQueryService serviceCatalog,
        IPartRepository partRepository, IAICircuitBreaker circuitBreaker,
        ILogger<AIServiceClient> logger)
    {
        _httpClient = httpClient;
        _serviceCatalog = serviceCatalog;
        _partRepository = partRepository;
        _circuitBreaker = circuitBreaker;
        _logger = logger;
    }

    public async Task<AIAdviceResult> GetAdviceAsync(AIAdviceRequest request, CancellationToken cancellationToken = default)
    {
        // Task 6.20 — kiểm tra Circuit Breaker TRƯỚC KHI gọi HTTP, tránh lãng phí request
        // khi biết chắc AI Service đang lỗi liên tục.
        if (!_circuitBreaker.CanExecute())
        {
            _logger.LogWarning("AI Circuit Breaker đang OPEN — bỏ qua gọi AI Service.");
            return Unavailable("Tính năng tư vấn AI tạm thời không khả dụng, vui lòng thử lại.");
        }

        var services = await _serviceCatalog.GetAvailableServicesAsync(request.DeviceType);
        var parts = await _serviceCatalog.GetAvailablePartsAsync();

        var httpRequest = new AdvisoryHttpRequest(
            RequestId: Guid.NewGuid(),
            Device: new AdvisoryDeviceDto(request.DeviceType.ToLowerInvariant(), request.Brand, request.Model),
            IssueDescription: request.IssueDescription,
            Context: new AdvisoryContextDto(
                services.Select(s => new AdvisoryServiceDto(s.ServiceId, s.Name, s.DeviceType, s.BasePrice)).ToList(),
                parts.Select(p => new AdvisoryPartDto(p.PartId, p.Name, p.UnitPrice)).ToList()));

        try
        {
            var httpResponse = await _httpClient.PostAsJsonAsync("/v1/advisory", httpRequest, cancellationToken);
            return await HandleResponseAsync(httpResponse, httpRequest.RequestId, services, parts, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // 408 — Backend-side timeout (HttpClient.Timeout vượt ngưỡng, Task 6.19)
            _logger.LogWarning(ex, "AI_TIMEOUT (client-side) cho request {RequestId}", httpRequest.RequestId);
            _circuitBreaker.RecordFailure();
            return Unavailable("Tính năng tư vấn AI tạm thời không khả dụng, vui lòng thử lại.");
        }
        catch (HttpRequestException ex)
        {
            // Không kết nối được (DNS/refused/network) — coi như AI_SERVICE_UNAVAILABLE
            _logger.LogError(ex, "AI_SERVICE_UNAVAILABLE — không thể kết nối AI Service.");
            _circuitBreaker.RecordFailure();
            return Unavailable("Tính năng tư vấn AI tạm thời không khả dụng, vui lòng thử lại.");
        }
    }

    /// <summary>
    /// Validate ngược theo đúng yêu cầu Task 6.16 và Task 5 Tuần 2 (mục 6):
    /// 1) serviceId AI trả về phải tồn tại trong catalog THẬT đã gửi đi -> không tồn tại thì loại bỏ.
    /// 2) partId AI trả về phải tồn tại trong catalog THẬT -> không tồn tại thì loại bỏ; đồng thời
    ///    re-verify trực tiếp qua IPartRepository (không chỉ tin snapshot context trong bộ nhớ).
    /// 3) priceRange nếu lệch bất thường so với dữ liệu gốc (basePrice/unitPrice) -> log cảnh báo
    ///    (soft check — không loại bỏ toàn bộ suggestion, vì SUCCESS luôn phải có priceRange).
    /// </summary>
    private async Task<AIAdviceResult> ValidateAndMapAsync(
        AdvisoryHttpResponse body, List<CatalogServiceItem> knownServices, List<CatalogPartItem> knownParts)
    {
        var status = body.Status switch
        {
            "SUCCESS" => AIAdvisoryStatus.Success,
            "NO_MATCH" => AIAdvisoryStatus.NoMatch,
            "OUT_OF_SCOPE" => AIAdvisoryStatus.OutOfScope,
            _ => AIAdvisoryStatus.Unavailable,
        };

        if (status != AIAdvisoryStatus.Success)
        {
            return new AIAdviceResult(status, [], [], null, body.Reason, body.Disclaimer);
        }

        var serviceById = knownServices.ToDictionary(s => s.ServiceId);
        var validatedServices = new List<AISuggestedService>();

        foreach (var suggested in body.SuggestedServices)
        {
            if (!serviceById.TryGetValue(suggested.ServiceId, out var known))
            {
                // ❌ AI hallucinate — serviceId không nằm trong dữ liệu THẬT đã gửi đi -> loại bỏ,
                // KHÔNG hiển thị cho người dùng dù AI có "tự tin" trả về đến đâu.
                _logger.LogWarning(
                    "AI trả về serviceId '{ServiceId}' không tồn tại trong catalog thật — đã loại bỏ.",
                    suggested.ServiceId);
                continue;
            }

            validatedServices.Add(new AISuggestedService(known.ServiceId, known.Name, suggested.Confidence));
        }

        var partById = knownParts.ToDictionary(p => p.PartId);
        var validatedParts = new List<AISuggestedPart>();

        foreach (var suggested in body.SuggestedParts)
        {
            if (!partById.TryGetValue(suggested.PartId, out var known))
            {
                _logger.LogWarning(
                    "AI trả về partId '{PartId}' không tồn tại trong catalog thật — đã loại bỏ.",
                    suggested.PartId);
                continue;
            }

            // Re-verify trực tiếp DB — phòng ngừa trường hợp Part vừa bị xoá/đổi ngay giữa lúc
            // build context và lúc AI phản hồi (race condition hiếm nhưng đúng tinh thần
            // "Backend không tin tưởng tuyệt đối", Task 5 Tuần 2 mục 6).
            if (Guid.TryParse(known.PartId, out var partGuid))
            {
                var freshPart = await _partRepository.GetByIdAsync(partGuid);
                if (freshPart is null)
                {
                    _logger.LogWarning(
                        "PartId '{PartId}' từng hợp lệ lúc build context nhưng không còn tồn tại trong DB — đã loại bỏ.",
                        known.PartId);
                    continue;
                }
            }

            validatedParts.Add(new AISuggestedPart(known.PartId, known.Name));
        }

        // Sanity check giá — soft warning, không loại bỏ suggestion (Task 5 Tuần 2 mục 6:
        // "nếu lệch bất thường, log cảnh báo và không hiển thị" — áp dụng ở mức cảnh báo vận hành,
        // KHÔNG huỷ toàn bộ kết quả để tránh chặn nhầm những trường hợp giá hợp lý nhưng khác biệt
        // do cách LLM làm tròn số).
        if (body.PriceRange is not null)
        {
            var referencePrice = validatedServices
                .Select(s => serviceById.GetValueOrDefault(s.ServiceId)?.BasePrice)
                .Concat(validatedParts.Select(p => partById.GetValueOrDefault(p.PartId)?.UnitPrice))
                .Where(p => p is not null)
                .Select(p => p!.Value)
                .DefaultIfEmpty(0)
                .Sum();

            if (referencePrice > 0)
            {
                var lowerBound = referencePrice * 0.5m;
                var upperBound = referencePrice * 2m;

                if (body.PriceRange.Min < lowerBound || body.PriceRange.Max > upperBound)
                {
                    _logger.LogWarning(
                        "PriceRange AI trả về [{Min}-{Max}] lệch bất thường so với giá tham chiếu {ReferencePrice} — cần rà soát.",
                        body.PriceRange.Min, body.PriceRange.Max, referencePrice);
                }
            }
        }

        var validatedPriceRange = AIPriceRangeValidator.ValidateOrDiscard(
            body.PriceRange is null ? null : new AIPriceRange(body.PriceRange.Min,
            body.PriceRange.Max),
            validatedServices, validatedParts, knownServices, knownParts, _logger);

        // Nếu sau khi loại bỏ hết ID hallucinate mà không còn gì hợp lệ -> coi như NO_MATCH,
        // KHÔNG trả SUCCESS với 2 mảng rỗng (sẽ gây hiểu nhầm "AI tìm thấy nhưng rỗng").
        if (validatedServices.Count == 0 && validatedParts.Count == 0)
        {
            return new AIAdviceResult(
                AIAdvisoryStatus.NoMatch, [], [], null,
                "AI đề xuất không khớp với dữ liệu hiện có của cửa hàng.", null);
        }

        return new AIAdviceResult(
            AIAdvisoryStatus.Success,
            validatedServices,
            validatedParts,
            validatedPriceRange,
            body.Reason,
            body.Disclaimer);
    }

    /// <summary>
    /// Mapping đầy đủ 5 mã lỗi theo contract Task 5 Tuần 2 (mục 5):
    /// 400 INVALID_REQUEST | 401 INVALID_API_KEY | 408 AI_TIMEOUT | 500 AI_INTERNAL_ERROR | 503 AI_SERVICE_UNAVAILABLE
    /// </summary>
    private async Task<AIAdviceResult> HandleResponseAsync(
        HttpResponseMessage httpResponse, Guid requestId,
        List<CatalogServiceItem> services, List<CatalogPartItem> parts,
        CancellationToken cancellationToken)
    {
        switch (httpResponse.StatusCode)
        {
            case HttpStatusCode.OK:
                _circuitBreaker.RecordSuccess(); // Task 6.20 — request thành công, reset trạng thái lỗi
                var body = await httpResponse.Content.ReadFromJsonAsync<AdvisoryHttpResponse>(cancellationToken: cancellationToken)
                    ?? throw new InvalidOperationException("AI Service trả về response rỗng.");
                return await ValidateAndMapAsync(body, services, parts);

            case HttpStatusCode.BadRequest: // 400 INVALID_REQUEST
                // Lỗi DO BACKEND gửi sai định dạng request lên FastAPI — đây là bug thật của
                // Backend (không phải lỗi AI Service), log ERROR để dev soát lại code gửi request,
                // KHÔNG gọi lại AI (retry vô ích vì request sai sẽ luôn sai).
                _logger.LogError("INVALID_REQUEST khi gọi AI Service cho request {RequestId} — kiểm tra lại payload gửi đi.", requestId);
                return Unavailable("Tính năng tư vấn AI tạm thời không khả dụng, vui lòng thử lại.");

            case HttpStatusCode.Unauthorized: // 401 INVALID_API_KEY
                // Log cảnh báo bảo mật NGHIÊM TRỌNG — sai/thiếu API Key nội bộ nghĩa là cấu hình
                // giữa Backend và AI Service không khớp (hoặc bị lộ/thay đổi key). Task 5 Tuần 2:
                // "Log cảnh báo bảo mật, không expose lỗi này cho Frontend".
                _logger.LogCritical("INVALID_API_KEY khi gọi AI Service cho request {RequestId} — kiểm tra cấu hình AIService:InternalApiKey.", requestId);
                return Unavailable("Tính năng tư vấn AI tạm thời không khả dụng, vui lòng thử lại.");

            case HttpStatusCode.RequestTimeout: // 408 AI_TIMEOUT
                _logger.LogWarning("AI_TIMEOUT (server-side) cho request {RequestId}", requestId);
                _circuitBreaker.RecordFailure();
                return Unavailable("Tính năng tư vấn AI tạm thời không khả dụng, vui lòng thử lại.");

            case HttpStatusCode.InternalServerError: // 500 AI_INTERNAL_ERROR
                _logger.LogError("AI_INTERNAL_ERROR cho request {RequestId} — lỗi nội bộ FastAPI.", requestId);
                _circuitBreaker.RecordFailure();
                return Unavailable("Tính năng tư vấn AI tạm thời không khả dụng, vui lòng thử lại.");

            case HttpStatusCode.ServiceUnavailable: // 503 AI_SERVICE_UNAVAILABLE
                _logger.LogWarning("AI_SERVICE_UNAVAILABLE cho request {RequestId} — AI Service đang down.", requestId);
                _circuitBreaker.RecordFailure();
                return Unavailable("Tính năng tư vấn AI tạm thời không khả dụng, vui lòng thử lại.");

            default:
                _logger.LogWarning("AI Service trả mã lỗi không xác định {StatusCode} cho request {RequestId}",
                    httpResponse.StatusCode, requestId);
                _circuitBreaker.RecordFailure();
                return Unavailable("Tính năng tư vấn AI tạm thời không khả dụng, vui lòng thử lại.");
        }
    }

    private static AIAdviceResult Unavailable(string reason) =>
        new(AIAdvisoryStatus.Unavailable, [], [], null, reason, null);
}