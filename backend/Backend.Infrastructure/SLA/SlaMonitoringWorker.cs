using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Enums;
using RepairShop.Infrastructure.Persistence;

namespace RepairShop.Infrastructure.SLA;

public sealed class SlaMonitoringWorker(
    IServiceScopeFactory scopes,
    ILogger<SlaMonitoringWorker> logger)
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
                var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
                var records = await db.TicketSLAs
                    .Where(x => x.Status != SLAStatus.Completed)
                    .Include(x => x.RepairTicket)
                    .ThenInclude(ticket => ticket.Technician)
                    .Take(500).ToListAsync(stoppingToken);
                foreach (var record in records)
                {
                    try
                    {
                        var becameOverdue = now >= record.DueAt && record.MarkOverdue(now);
                        if (!becameOverdue)
                        {
                            record.RefreshStatus(now);
                            continue;
                        }

                        var ticket = record.RepairTicket;
                        const string type = "SLA_OVERDUE";
                        var title = $"Phiếu {ticket.TicketCode} đã quá hạn SLA";
                        var message = $"Phiếu sửa chữa {ticket.TicketCode} đã vượt thời hạn xử lý SLA.";
                        await notifications.CreateForRoleAsync(
                            Roles.Admin, type, title, message, "RepairTicket", ticket.Id);
                        await notifications.CreateForRoleAsync(
                            Roles.Receptionist, type, title, message, "RepairTicket", ticket.Id);
                        if (ticket.TechnicianId is Guid technicianId)
                            await notifications.CreateAsync(
                                technicianId, type, title, message, "RepairTicket", ticket.Id);
                    }
                    catch (Exception ex)
                    {
                        record.ReleaseOverdueNotification();
                        logger.LogError(ex, "SLA record {SlaId} failed during monitoring.", record.Id);
                    }
                }
                await db.SaveChangesAsync(stoppingToken);
                logger.LogInformation("SLA monitoring checked {Count} records.", records.Count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "SLA monitoring cycle failed."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
