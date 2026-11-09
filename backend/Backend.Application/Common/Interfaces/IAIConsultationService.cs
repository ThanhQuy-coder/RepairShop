using RepairShop.Application.Modules.AIAdvisory.DTOs;

namespace RepairShop.Application.Common.Interfaces;

/// <summary>
/// Gọi AI Service cho luồng MAS (Task 4.1). Implementation chịu trách nhiệm:
/// dựng context, gọi FastAPI, hậu kiểm kết quả và **không bao giờ throw** ra ngoài.
/// </summary>
public interface IAIConsultationService
{
    Task<ConsultationResponse> GetConsultationAsync(
        ConsultationRequest request,
        CancellationToken cancellationToken = default);
}
