using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Modules.Appointments;
using Microsoft.EntityFrameworkCore;

namespace RepairShop.Infrastructure.Persistence.Repositories;

public class TimeSlotConfigRepository : ITimeSlotConfigRepository
{
    private readonly AppDbContext _context;
    public TimeSlotConfigRepository(AppDbContext context) => _context = context;

    public Task<TimeSlotConfig?> GetByIdAsync(Guid id) => _context.TimeSlotConfigs.FirstOrDefaultAsync(t => t.Id == id);
    public Task<List<TimeSlotConfig>> ListAsync() => _context.TimeSlotConfigs.OrderBy(t => t.SlotStart).ToListAsync();
    public async Task AddAsync(TimeSlotConfig slot) => await _context.TimeSlotConfigs.AddAsync(slot);
    public void Update(TimeSlotConfig slot) => _context.TimeSlotConfigs.Update(slot);
    public void Remove(TimeSlotConfig slot) => _context.TimeSlotConfigs.Remove(slot);
    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}