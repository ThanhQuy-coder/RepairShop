using Backend.Application.Common.Interfaces;
using Backend.Domain.Modules.Devices;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence.Repositories;

public class DeviceRepository : IDeviceRepository
{
    private readonly AppDbContext _context;

    public DeviceRepository(AppDbContext context) => _context = context;

    public Task<Device?> GetByIdAsync(Guid id) =>
        _context.Devices.Include(d => d.Customer).FirstOrDefaultAsync(d => d.Id == id);

    public Task<List<Device>> GetByCustomerIdAsync(Guid customerId) =>
        _context.Devices.Where(d => d.CustomerId == customerId).ToListAsync();

    public async Task AddAsync(Device device) => await _context.Devices.AddAsync(device);

    public void Update(Device device) => _context.Devices.Update(device);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}