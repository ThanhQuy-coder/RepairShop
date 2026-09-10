using RepairShop.Application.Modules.Reports.Queries;
using RepairShop.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)] // FR-057->060: chỉ Admin xem báo cáo
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ReportsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("dashboard-summary")]
    public async Task<IActionResult> GetDashboardSummary()
    {
        var result = await _mediator.Send(new GetDashboardSummaryQuery());
        return Ok(result);
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenue(
    [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] string groupBy = "day")
    {
        var parsedGroupBy = groupBy.Equals("month", StringComparison.OrdinalIgnoreCase)
            ? RevenueGroupBy.Month : RevenueGroupBy.Day;

        var result = await _mediator.Send(new GetRevenueReportQuery(fromDate, toDate, parsedGroupBy));
        return Ok(result);
    }

    [HttpGet("technician-performance")]
    public async Task<IActionResult> GetTechnicianPerformance([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var result = await _mediator.Send(new GetTechnicianPerformanceQuery(fromDate, toDate));
        return Ok(result);
    }
}