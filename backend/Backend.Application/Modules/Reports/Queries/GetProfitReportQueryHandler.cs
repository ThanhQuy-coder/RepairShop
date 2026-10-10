using MediatR;
using RepairShop.Application.Common.Interfaces;

namespace RepairShop.Application.Modules.Reports.Queries;

public sealed class GetProfitReportQueryHandler(IReportsQueryService reportsQueryService)
    : IRequestHandler<GetProfitReportQuery, ProfitReportResponse>
{
    public Task<ProfitReportResponse> Handle(
        GetProfitReportQuery request, CancellationToken cancellationToken)
        => reportsQueryService.GetProfitReportAsync(request.FromDate, request.ToDate);
}
