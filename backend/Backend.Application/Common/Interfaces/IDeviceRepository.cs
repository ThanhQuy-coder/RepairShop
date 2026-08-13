using Backend.Domain.Modules.Devices;

namespace Backend.Application.Common.Interfaces;

public interface IDeviceRepository
{
    Task<Device?> GetByIdAsync(Guid id);
    Task<List<Device>> GetByCustomerIdAsync(Guid customerId);
    Task AddAsync(Device device);
    void Update(Device device);
    Task SaveChangesAsync();
}