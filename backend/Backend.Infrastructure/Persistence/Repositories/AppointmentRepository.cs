using RepairShop.Domain.Modules.Appointments;
using RepairShop.Domain.Modules.Appointments.Enums;
using Microsoft.EntityFrameworkCore;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Appointments.DTOs;
using RepairShop.Domain.Common.Exceptions;

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
                a.SerialNumber,
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

    public async Task<AppointmentDetailResponse?> GetByLookupAsync(
        string lookupTokenHash, string phone)
    {
        return await (
            from a in _context.Appointments
            join ts in _context.TimeSlotConfigs on a.TimeSlotId equals ts.Id
            where a.LookupTokenHash == lookupTokenHash && a.Phone == phone
            select new AppointmentDetailResponse(
                a.Id,
                a.FullName,
                a.Phone,
                a.DeviceType,
                a.Brand,
                a.Model,
                a.SerialNumber,
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

    public async Task<int> UpdateCustomerIdAsync(string phone, Guid customerId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Số điện thoại không được hợp lệ.", nameof(phone));

        try
        {
            int affectedRows = await _context.Appointments
                .Where(a => a.Phone == phone && a.CustomerId == null)
                .ExecuteUpdateAsync(
                    a => a.SetProperty(x => x.CustomerId, customerId),
                    cancellationToken
                );

            if (affectedRows == 0)
            {
                throw new DomainException("Không tìm thấy cuộc hẹn nào phù hợp cho Phone: {Phone}");
            }

            return affectedRows;
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Thao tác cập nhật CustomerId bị timeout do hệ thống đang quá tải.", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Xảy ra lỗi kết nối hoặc xung đột dữ liệu khi cập nhật CustomerId.", ex);
        }
        catch (Exception ex)
        {
            throw new ApplicationException("Sự cố hệ thống không xác định trong quá trình cập nhật CustomerId.", ex);
        }
    }

    public Task<List<Appointment>> GetByPhoneAsync(string phone)
    {
        Console.WriteLine($"GetByPhoneAsync called with phone: {phone}");
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Số điện thoại không được hợp lệ.", nameof(phone));

        return _context.Appointments
            .Where(a => a.Phone == phone)
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();
    }
}