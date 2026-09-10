using MediatR;

namespace RepairShop.Application.Modules.Reports.Queries;

public record GetTechnicianPerformanceQuery(DateTime? FromDate, DateTime? ToDate)
    : IRequest<List<TechnicianSummaryItem>>;

public class GetTechnicianPerformanceQueryHandler : IRequestHandler<GetTechnicianPerformanceQuery, List<TechnicianSummaryItem>>
{
    private readonly RepairShop.Application.Common.Interfaces.IReportsQueryService _reportsQueryService;
    public GetTechnicianPerformanceQueryHandler(RepairShop.Application.Common.Interfaces.IReportsQueryService reportsQueryService) =>
        _reportsQueryService = reportsQueryService;

    public Task<List<TechnicianSummaryItem>> Handle(GetTechnicianPerformanceQuery request, CancellationToken cancellationToken) =>
        _reportsQueryService.GetTechnicianPerformanceAsync(request.FromDate, request.ToDate);
}