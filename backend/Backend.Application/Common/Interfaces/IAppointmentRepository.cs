using RepairShop.Domain.Modules.Appointments;

namespace RepairShop.Application.Common.Interfaces;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid id);
    Task<List<Appointment>> GetByCustomerIdAsync(Guid customerId);
    Task<int> CountBookedAsync(DateOnly date, Guid timeSlotId); // PENDING + CONFIRMED
    Task AddAsync(Appointment appointment);
    void Update(Appointment appointment);
    Task SaveChangesAsync();
    Task AcquireSlotLockAsync(DateOnly date, Guid timeSlotId, CancellationToken cancellationToken);
}