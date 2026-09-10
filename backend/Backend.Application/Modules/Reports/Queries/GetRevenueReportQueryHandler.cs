using RepairShop.Application.Common.Interfaces;
using MediatR;

namespace RepairShop.Application.Modules.Reports.Queries;

public class GetRevenueReportQueryHandler : IRequestHandler<GetRevenueReportQuery, RevenueReportResponse>
{
    private readonly IReportsQueryService _reportsQueryService;
    public GetRevenueReportQueryHandler(IReportsQueryService reportsQueryService) => _reportsQueryService = reportsQueryService;

    public Task<RevenueReportResponse> Handle(GetRevenueReportQuery request, CancellationToken cancellationToken) =>
        _reportsQueryService.GetRevenueReportAsync(request.FromDate, request.ToDate, request.GroupBy);
}