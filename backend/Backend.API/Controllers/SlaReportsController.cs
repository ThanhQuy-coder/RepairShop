using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RepairShop.Domain.Common.Enums;
using RepairShop.Infrastructure.Identity;
using RepairShop.Infrastructure.Persistence;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/reports/sla-summary")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class SlaReportsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SlaSummaryResponse>> Get(CancellationToken ct)
    {
        var active = db.TicketSLAs.Where(x => x.Status != SLAStatus.Completed);
        var counts = await active.GroupBy(x => x.Status)
            .Select(x => new { x.Key, Count = x.Count() }).ToListAsync(ct);
        int Count(SLAStatus status) => counts.FirstOrDefault(x => x.Key == status)?.Count ?? 0;
        return Ok(new SlaSummaryResponse(Count(SLAStatus.OnTrack), Count(SLAStatus.DueSoon),
            Count(SLAStatus.Overdue), counts.Sum(x => x.Count)));
    }

    public sealed record SlaSummaryResponse(int OnTrack, int DueSoon, int Overdue, int ActiveTotal);
}
