using RepairShop.Domain.Modules.Appointments;

namespace RepairShop.Application.Common.Interfaces;

public interface ITimeSlotConfigRepository
{
    Task<TimeSlotConfig?> GetByIdAsync(Guid id);
    Task<List<TimeSlotConfig>> ListAsync();
    Task AddAsync(TimeSlotConfig slot);
    void Update(TimeSlotConfig slot);
    void Remove(TimeSlotConfig slot);
    Task SaveChangesAsync();
}