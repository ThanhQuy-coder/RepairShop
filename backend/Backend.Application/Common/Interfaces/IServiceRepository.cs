using RepairShop.Domain.Modules.Content;

namespace RepairShop.Application.Common.Interfaces;

public interface IServiceRepository
{
    Task<Service?> GetByIdAsync(Guid id);
    Task<List<Service>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task<(List<Service> Items, int Total)> SearchAsync(
        bool? isActive,
        int page,
        int pageSize,
        string? deviceType = null,
        string? search = null);
    Task<Dictionary<Guid, (double AverageRating, int ReviewCount)>> GetServiceRatingStatsAsync(IEnumerable<Guid> serviceIds);
    Task AddAsync(Service service);
    void Update(Service service);
    Task SaveChangesAsync();
}
