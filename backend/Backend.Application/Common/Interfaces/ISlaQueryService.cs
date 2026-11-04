using RepairShop.Application.Modules.SLA.DTOs;
using RepairShop.Domain.Common.Enums;

namespace RepairShop.Application.Common.Interfaces;

public interface ISlaQueryService
{
    Task<SlaSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken);
    Task<SlaTicketListResponse> GetTicketsAsync(
        SLAStatus? slaStatus, string? statusCode, DeviceType? deviceType,
        int page, int pageSize, CancellationToken cancellationToken);
    Task<SlaAnalyticsResponse> GetAnalyticsAsync(CancellationToken cancellationToken);
    Task<SlaMyTicketsResponse> GetMyTicketsAsync(CancellationToken cancellationToken);
}
