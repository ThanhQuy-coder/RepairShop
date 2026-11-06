using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Modules.Devices;

namespace RepairShop.Application.Common.Interfaces;

public interface IBrandRepository
{
    Task<Brand?> GetByIdAsync(Guid id);
    Task<Brand?> GetByCodeAsync(string code);
    Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null);
    Task<bool> IsReferencedAsync(Guid brandId);
    Task<(List<Brand> Items, int Total, Dictionary<Guid, List<string>> DeviceTypesByBrand, Dictionary<Guid, int> ModelCountByBrand)> SearchAsync(
        DeviceType? deviceType,
        bool? isActive,
        string? search,
        int page,
        int pageSize);
    Task AddAsync(Brand brand);
    void Update(Brand brand);
    Task SaveChangesAsync();
}
