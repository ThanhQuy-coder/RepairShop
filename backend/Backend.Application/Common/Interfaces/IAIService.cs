using RepairShop.Application.Modules.AIAdvisory.DTOs;

namespace RepairShop.Application.Common.Interfaces;

/// <summary>
/// Application CHỈ biết interface này — không biết HttpClient, FastAPI URL, hay API Key nội bộ.
/// Infrastructure (AIServiceClient) chịu trách nhiệm implement toàn bộ chi tiết kỹ thuật.
/// Đúng kiến trúc đã chốt Tuần 2: Application/AIAdvisory chỉ chứa Commands/DTOs/Validators,
/// Infrastructure/AI chứa AIServiceClient thật.
/// </summary>
public interface IAIService
{
    Task<AIAdviceResult> GetAdviceAsync(AIAdviceRequest request, CancellationToken cancellationToken = default);
}