using Microsoft.EntityFrameworkCore;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Modules.Devices;

namespace RepairShop.Infrastructure.Persistence.Repositories;

public class BrandRepository : IBrandRepository
{
    private readonly AppDbContext _context;

    public BrandRepository(AppDbContext context) => _context = context;

    public Task<Brand?> GetByIdAsync(Guid id) =>
        _context.Brands.FirstOrDefaultAsync(b => b.Id == id && b.DeletedAt == null);

    public Task<Brand?> GetByCodeAsync(string code)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return _context.Brands.FirstOrDefaultAsync(b => b.Code == normalized && b.DeletedAt == null);
    }

    public Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return _context.Brands.AnyAsync(b =>
            b.DeletedAt == null &&
            b.Code == normalized &&
            (!excludeId.HasValue || b.Id != excludeId.Value));
    }

    public Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null)
    {
        var normalized = name.Trim().ToLower();
        return _context.Brands.AnyAsync(b =>
            b.DeletedAt == null &&
            b.Name.ToLower() == normalized &&
            (!excludeId.HasValue || b.Id != excludeId.Value));
    }

    public async Task<bool> IsReferencedAsync(Guid brandId)
    {
        var brand = await GetByIdAsync(brandId);
        if (brand is null) return false;

        var hasModels = await _context.DeviceModels.AnyAsync(m => m.BrandId == brandId && m.DeletedAt == null);
        if (hasModels) return true;

        var brandNameLower = brand.Name.ToLower();
        return await _context.Devices.AnyAsync(d =>
            d.DeletedAt == null &&
            d.Brand.ToLower() == brandNameLower);
    }

    public async Task<(List<Brand> Items, int Total, Dictionary<Guid, List<string>> DeviceTypesByBrand, Dictionary<Guid, int> ModelCountByBrand)> SearchAsync(
        DeviceType? deviceType,
        bool? isActive,
        string? search,
        int page,
        int pageSize)
    {
        var query = _context.Brands
            .Where(b => b.DeletedAt == null)
            .AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(b => b.IsActive == isActive.Value);
        }

        if (deviceType.HasValue)
        {
            var dt = deviceType.Value;
            var onlyActiveModels = isActive == true;
            query = query.Where(b => _context.DeviceModels.Any(m =>
                m.BrandId == b.Id &&
                m.DeletedAt == null &&
                m.DeviceType == dt &&
                (!onlyActiveModels || m.IsActive)));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(b =>
                b.Name.ToLower().Contains(keyword) ||
                b.Code.ToLower().Contains(keyword) ||
                (b.Description != null && b.Description.ToLower().Contains(keyword)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(b => b.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var brandIds = items.Select(b => b.Id).ToList();
        var modelStats = brandIds.Count == 0
            ? new List<(Guid BrandId, DeviceType DeviceType, bool IsActive)>()
            : (await _context.DeviceModels
                .Where(m => brandIds.Contains(m.BrandId) && m.DeletedAt == null)
                .Select(m => new { m.BrandId, m.DeviceType, m.IsActive })
                .ToListAsync())
                .Select(x => (x.BrandId, x.DeviceType, x.IsActive))
                .ToList();

        var deviceTypesByBrand = brandIds.ToDictionary(
            id => id,
            id => modelStats
                .Where(m => m.BrandId == id && (isActive != true || m.IsActive))
                .Select(m => m.DeviceType.ToString())
                .Distinct()
                .OrderBy(x => x)
                .ToList());

        var modelCountByBrand = brandIds.ToDictionary(
            id => id,
            id => modelStats.Count(m => m.BrandId == id && (isActive != true || m.IsActive)));

        return (items, total, deviceTypesByBrand, modelCountByBrand);
    }

    public async Task AddAsync(Brand brand) => await _context.Brands.AddAsync(brand);

    public void Update(Brand brand) => _context.Brands.Update(brand);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
