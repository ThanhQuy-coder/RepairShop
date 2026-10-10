using Microsoft.EntityFrameworkCore;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.SLA.DTOs;
using RepairShop.Domain.Common.Enums;

namespace RepairShop.Infrastructure.Persistence.Repositories;

public sealed class SlaQueryService(AppDbContext context) : ISlaQueryService
{
    public async Task<SlaSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var counts = await context.TicketSLAs
            .Where(sla => sla.Status != SLAStatus.Completed)
            .GroupBy(sla => sla.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        int Count(SLAStatus status)
            => counts.FirstOrDefault(item => item.Status == status)?.Count ?? 0;

        return new SlaSummaryResponse(
            Count(SLAStatus.OnTrack),
            Count(SLAStatus.DueSoon),
            Count(SLAStatus.Overdue),
            counts.Sum(item => item.Count));
    }
}
