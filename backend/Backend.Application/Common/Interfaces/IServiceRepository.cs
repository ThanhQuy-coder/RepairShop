using RepairShop.Domain.Modules.Content;

namespace RepairShop.Application.Common.Interfaces;

public interface IServiceRepository
{
    Task<Service?> GetByIdAsync(Guid id);
    Task<(List<Service> Items, int Total)> SearchAsync(bool? isActive, int page, int pageSize);
    Task AddAsync(Service service);
    void Update(Service service);
    Task SaveChangesAsync();
}