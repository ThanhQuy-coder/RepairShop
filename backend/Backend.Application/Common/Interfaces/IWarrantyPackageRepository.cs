using RepairShop.Domain.Modules.Warranty;

namespace RepairShop.Application.Common.Interfaces;

public interface IWarrantyPackageRepository
{
    Task<WarrantyPackage?> GetByIdAsync(Guid id);
    Task<List<WarrantyPackage>> SearchAsync(bool? isActive);
    Task<bool> IsUsedAsync(Guid id);
    Task AddAsync(WarrantyPackage package);
    void Update(WarrantyPackage package);
    Task SaveChangesAsync();
}
