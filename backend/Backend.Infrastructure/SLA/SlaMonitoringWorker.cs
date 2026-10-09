using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RepairShop.Domain.Common.Enums;
using RepairShop.Infrastructure.Persistence;

namespace RepairShop.Infrastructure.SLA;

public sealed class SlaMonitoringWorker(IServiceScopeFactory scopes, ILogger<SlaMonitoringWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var now = DateTime.UtcNow;
                var records = await db.TicketSLAs
                    .Where(x => x.Status != SLAStatus.Completed)
                    .Take(500).ToListAsync(stoppingToken);
                foreach (var record in records)
                {
                    if (now >= record.DueAt) record.MarkOverdue(now);
                    else record.RefreshStatus(now);
                }
                await db.SaveChangesAsync(stoppingToken);
                logger.LogInformation("SLA monitoring checked {Count} records.", records.Count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "SLA monitoring cycle failed."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
