using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.AIAdvisory.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace RepairShop.Application.Modules.AIAdvisory.Commands.GetAIAdvice;

public class GetAIAdviceCommandHandler : IRequestHandler<GetAIAdviceCommand, AIAdviceResponse>
{
    private const string DefaultDisclaimer =
        "Đây là gợi ý tham khảo, giá chính thức do nhân viên xác nhận.";

    private readonly IAIService _aiService;
    private readonly IServiceCatalogQueryService _serviceCatalog; // xem mục 4 bên dưới
    private readonly ILogger<GetAIAdviceCommandHandler> _logger;

    public GetAIAdviceCommandHandler(
        IAIService aiService,
        IServiceCatalogQueryService serviceCatalog,
        ILogger<GetAIAdviceCommandHandler> logger)
    {
        _aiService = aiService;
        _serviceCatalog = serviceCatalog;
        _logger = logger;
    }

    public async Task<AIAdviceResponse> Handle(GetAIAdviceCommand request, CancellationToken cancellationToken)
    {
        var aiRequest = new AIAdviceRequest(request.DeviceType, request.Brand, request.Model, request.IssueDescription);

        AIAdviceResult result;
        try
        {
            result = await _aiService.GetAdviceAsync(aiRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            // AI Service down/timeout -> KHÔNG để lỗi AI làm gãy luồng chính (Task 5 Tuần 2:
            // "AI Service là tính năng hỗ trợ non-critical"). Trả về aiAvailable=false, không throw.
            _logger.LogWarning(ex, "AI Service không khả dụng khi xử lý tư vấn cho {DeviceType} {Brand} {Model}",
                request.DeviceType, request.Brand, request.Model);

            return new AIAdviceResponse(
                AiAvailable: false, SuggestedServices: [], SuggestedParts: [],
                Explanation: null, Message: "Tính năng tư vấn AI tạm thời không khả dụng, vui lòng thử lại sau.",
                Disclaimer: DefaultDisclaimer);
        }

        return result.Status switch
        {
            AIAdvisoryStatus.Success => new AIAdviceResponse(
                AiAvailable: true,
                SuggestedServices: result.SuggestedServices
                    .Select(s => new AIAdviceServiceItem(
                        s.ServiceId, s.ServiceName,
                        result.PriceRange?.Min ?? 0, result.PriceRange?.Max ?? 0))
                    .ToList(),
                SuggestedParts: result.SuggestedParts.Select(p => p.PartName).ToList(),
                Explanation: result.Reason,
                Message: null,
                Disclaimer: result.Disclaimer ?? DefaultDisclaimer),

            AIAdvisoryStatus.NoMatch => new AIAdviceResponse(
                AiAvailable: true, SuggestedServices: [], SuggestedParts: [],
                Explanation: result.Reason,
                Message: "AI chưa đủ dữ liệu để tư vấn, vui lòng mang máy đến kiểm tra trực tiếp.",
                Disclaimer: DefaultDisclaimer),

            AIAdvisoryStatus.OutOfScope => new AIAdviceResponse(
                AiAvailable: true, SuggestedServices: [], SuggestedParts: [],
                Explanation: null,
                Message: "Câu hỏi nằm ngoài phạm vi tư vấn sửa chữa thiết bị.",
                Disclaimer: DefaultDisclaimer),

            _ => new AIAdviceResponse( // Unavailable
                AiAvailable: false, SuggestedServices: [], SuggestedParts: [],
                Explanation: null, Message: "Tính năng tư vấn AI tạm thời không khả dụng, vui lòng thử lại sau.",
                Disclaimer: DefaultDisclaimer),
        };
    }
}