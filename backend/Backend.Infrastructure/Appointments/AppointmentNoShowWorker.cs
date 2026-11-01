using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RepairShop.Domain.Modules.Appointments.Enums;
using RepairShop.Infrastructure.Persistence;
using RepairShop.Application.Common.Interfaces;

namespace RepairShop.Infrastructure.Appointments;

public sealed class AppointmentNoShowWorker(
    IServiceScopeFactory scopes,
    IConfiguration configuration,
    ILogger<AppointmentNoShowWorker> logger) : BackgroundService
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
                var clock = scope.ServiceProvider.GetRequiredService<IBusinessClock>();
                var grace = configuration.GetValue("Appointment:NoShowGraceMinutes", 15);
                var cutoff = clock.Now.DateTime.AddMinutes(-grace);
                var due = await (
                    from appointment in db.Appointments
                    join slot in db.TimeSlotConfigs on appointment.TimeSlotId equals slot.Id
                    where (appointment.Status == AppointmentStatus.Pending ||
                           appointment.Status == AppointmentStatus.Confirmed) &&
                          appointment.AppointmentDate.ToDateTime(slot.SlotEnd) <= cutoff
                    select appointment).Take(500).ToListAsync(stoppingToken);

                foreach (var appointment in due)
                    appointment.MarkNoShow();

                if (due.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
                logger.LogInformation("Appointment No-Show worker processed {Count} records.", due.Count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Appointment No-Show worker cycle failed."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
