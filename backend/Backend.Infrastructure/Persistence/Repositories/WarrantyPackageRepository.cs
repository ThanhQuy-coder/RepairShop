using Microsoft.EntityFrameworkCore;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Modules.Warranty;

namespace RepairShop.Infrastructure.Persistence.Repositories;

public class WarrantyPackageRepository(AppDbContext context) : IWarrantyPackageRepository
{
    public Task<WarrantyPackage?> GetByIdAsync(Guid id) =>
        context.WarrantyPackages.FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

    public Task<List<WarrantyPackage>> SearchAsync(bool? isActive)
    {
        var query = context.WarrantyPackages.Where(p => p.DeletedAt == null);
        if (isActive is not null) query = query.Where(p => p.IsActive == isActive);
        return query.OrderBy(p => p.DurationMonths).ThenBy(p => p.Name).ToListAsync();
    }

    public Task<bool> IsUsedAsync(Guid id) =>
        context.Warranties.AnyAsync(w => w.WarrantyPackageId == id);

    public async Task AddAsync(WarrantyPackage package) => await context.WarrantyPackages.AddAsync(package);
    public void Update(WarrantyPackage package) => context.WarrantyPackages.Update(package);
    public Task SaveChangesAsync() => context.SaveChangesAsync();
}
