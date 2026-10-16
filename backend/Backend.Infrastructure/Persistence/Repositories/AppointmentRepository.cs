using RepairShop.Domain.Modules.Appointments;
using RepairShop.Domain.Modules.Appointments.Enums;
using Microsoft.EntityFrameworkCore;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Appointments.DTOs;

namespace RepairShop.Infrastructure.Persistence.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly AppDbContext _context;
    public AppointmentRepository(AppDbContext context) => _context = context;

    public Task<Appointment?> GetByIdAsync(Guid id)
        => _context.Appointments.FirstOrDefaultAsync(a => a.Id == id);

    public async Task<AppointmentDetailResponse?> GetDetailByIdAsync(Guid id)
    {
        return await (
            from a in _context.Appointments
            join ts in _context.TimeSlotConfigs on a.TimeSlotId equals ts.Id
            where a.Id == id
            select new AppointmentDetailResponse(
                a.Id,
                a.FullName,
                a.Phone,
                a.DeviceType,
                a.Brand,
                a.Model,
                a.IssueDescription,
                a.AppointmentDate,
                a.TimeSlotId,
                ts.SlotStart,
                ts.SlotEnd,
                a.Status.ToString(),
                a.RejectionReason,
                a.CreatedAt
            )
        ).FirstOrDefaultAsync();
    }

    public Task<Appointment?> GetByLookupAsync(string lookupTokenHash, string phone) =>
        _context.Appointments.FirstOrDefaultAsync(a => a.LookupTokenHash == lookupTokenHash && a.Phone == phone);
    
    public Task<List<Appointment>> GetForStaffAsync() =>
        _context.Appointments
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();
    
    public Task<List<Appointment>> GetByCustomerIdAsync(Guid customerId) =>
        _context.Appointments.Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.CreatedAt).ToListAsync();

    public Task<int> CountBookedAsync(DateOnly date, Guid timeSlotId) =>
        _context.Appointments.CountAsync(a =>
            a.AppointmentDate == date && a.TimeSlotId == timeSlotId &&
            (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed));

    public async Task AddAsync(Appointment appointment) => await _context.Appointments.AddAsync(appointment);
    public void Update(Appointment appointment) => _context.Appointments.Update(appointment);
    public Task SaveChangesAsync() => _context.SaveChangesAsync();

    public async Task AcquireSlotLockAsync(DateOnly date, Guid timeSlotId, CancellationToken cancellationToken)
    {
        var lockKey = $"{date:yyyyMMdd}:{timeSlotId}";
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtext({lockKey}))", cancellationToken);
    }
}