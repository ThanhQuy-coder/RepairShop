using RepairShop.Application.Modules.SLA.DTOs;

namespace RepairShop.Application.Common.Interfaces;

public interface ISlaQueryService
{
    Task<SlaSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken);
}
