using MediatR;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.SLA.DTOs;

namespace RepairShop.Application.Modules.SLA.Queries;

public sealed class GetSlaSummaryQueryHandler(ISlaQueryService queryService)
    : IRequestHandler<GetSlaSummaryQuery, SlaSummaryResponse>
{
    public Task<SlaSummaryResponse> Handle(
        GetSlaSummaryQuery request, CancellationToken cancellationToken)
        => queryService.GetSummaryAsync(cancellationToken);
}
