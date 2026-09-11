using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Inventory.Queries;
using MediatR;

namespace RepairShop.Application.Modules.Reports.Queries;

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryResponse>
{
    private readonly IReportsQueryService _reportsQueryService;
    private readonly IMediator _mediator;

    public GetDashboardSummaryQueryHandler(IReportsQueryService reportsQueryService, IMediator mediator)
    {
        _reportsQueryService = reportsQueryService;
        _mediator = mediator;
    }

    public async Task<DashboardSummaryResponse> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var repair = await _reportsQueryService.GetRepairSummaryAsync();
        var revenue = await _reportsQueryService.GetRevenueSummaryAsync();
        var technicians = await _reportsQueryService.GetTechnicianSummaryAsync();
        var statusBreakdown = await _reportsQueryService.GetStatusBreakdownAsync();
        var totalCustomers = await _reportsQueryService.GetTotalCustomersAsync();

        var inventoryDashboard = await _mediator.Send(new GetInventoryDashboardQuery(), cancellationToken);
        var inventory = new InventorySummary(
            inventoryDashboard.TotalParts, inventoryDashboard.LowStockCount, 
            inventoryDashboard.OutOfStockCount);

        return new DashboardSummaryResponse(repair, revenue, technicians, 
            inventory, statusBreakdown, totalCustomers);
    }
}