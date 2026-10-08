using RepairShop.Domain.Modules.Appointments;
using RepairShop.Domain.Modules.Appointments.Enums;
using Microsoft.EntityFrameworkCore;
using RepairShop.Application.Common.Interfaces;

namespace RepairShop.Infrastructure.Persistence.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly AppDbContext _context;
    public AppointmentRepository(AppDbContext context) => _context = context;

    public Task<Appointment?> GetByIdAsync(Guid id) => _context.Appointments.FirstOrDefaultAsync(a => a.Id == id);
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

    /// <summary>
    /// BR-21 + NFR-025: khóa đồng bộ theo đúng cặp (date, timeSlotId) bằng advisory lock của PostgreSQL.
    /// Request thứ 2 gọi cùng slot+ngày sẽ BỊ CHẶN (chờ) tại đây cho tới khi request thứ 1 commit/rollback
    /// xong — đảm bảo COUNT + INSERT phía sau luôn thấy đúng dữ liệu mới nhất, không còn khoảng hở race.
    /// Lock tự giải phóng khi transaction kết thúc (pg_advisory_XACT_lock), không cần unlock thủ công.
    /// </summary>
    public async Task AcquireSlotLockAsync(DateOnly date, Guid timeSlotId, CancellationToken cancellationToken)
    {
        var lockKey = $"{date:yyyyMMdd}:{timeSlotId}";
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtext({lockKey}))", cancellationToken);
    }
}