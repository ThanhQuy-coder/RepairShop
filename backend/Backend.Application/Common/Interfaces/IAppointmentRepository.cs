using RepairShop.Application.Modules.Appointments.DTOs;
using RepairShop.Domain.Modules.Appointments;

namespace RepairShop.Application.Common.Interfaces;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid id);
    Task<AppointmentDetailResponse?> GetDetailByIdAsync(Guid id);
    Task<AppointmentDetailResponse?> GetByLookupAsync(string lookupTokenHash, string phone);
    Task<List<Appointment>> GetForStaffAsync();
    Task<List<Appointment>> GetByCustomerIdAsync(Guid customerId);
    Task<List<Appointment>> GetByPhoneAsync(string phone);
    Task<int> CountBookedAsync(DateOnly date, Guid timeSlotId); // PENDING + CONFIRMED
    Task AddAsync(Appointment appointment);
    void Update(Appointment appointment);
    Task<int> UpdateCustomerIdAsync(string phone, Guid customerId,
        CancellationToken cancellationToken = default);
    Task SaveChangesAsync();
    Task AcquireSlotLockAsync(DateOnly date, Guid timeSlotId, CancellationToken cancellationToken);
}