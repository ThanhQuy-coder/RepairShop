using Microsoft.EntityFrameworkCore;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Modules.Devices;

namespace RepairShop.Infrastructure.Persistence.Repositories;

public class DeviceModelRepository : IDeviceModelRepository
{
    private readonly AppDbContext _context;

    public DeviceModelRepository(AppDbContext context) => _context = context;

    public Task<DeviceModel?> GetByIdAsync(Guid id) =>
        _context.DeviceModels
            .Include(m => m.Brand)
            .FirstOrDefaultAsync(m => m.Id == id && m.DeletedAt == null);

    public Task<DeviceModel?> FindMatchAsync(DeviceType deviceType, string brandName, string modelName)
    {
        var brandNorm = brandName.Trim().ToLower();
        var modelNorm = modelName.Trim().ToLower();
        return _context.DeviceModels
            .Include(m => m.Brand)
            .FirstOrDefaultAsync(m =>
                m.DeletedAt == null &&
                m.Brand.DeletedAt == null &&
                m.DeviceType == deviceType &&
                m.Brand.Name.ToLower() == brandNorm &&
                m.ModelName.ToLower() == modelNorm);
    }

    public Task<bool> ExistsAsync(Guid brandId, DeviceType deviceType, string modelName, Guid? excludeId = null)
    {
        var normalized = modelName.Trim().ToLower();
        return _context.DeviceModels.AnyAsync(m =>
            m.DeletedAt == null &&
            m.BrandId == brandId &&
            m.DeviceType == deviceType &&
            m.ModelName.ToLower() == normalized &&
            (!excludeId.HasValue || m.Id != excludeId.Value));
    }

    public async Task<bool> IsReferencedAsync(Guid deviceModelId)
    {
        var model = await GetByIdAsync(deviceModelId);
        if (model is null) return false;

        var brandNameLower = model.Brand.Name.ToLower();
        var modelNameLower = model.ModelName.ToLower();

        return await _context.Devices.AnyAsync(d =>
            d.DeletedAt == null &&
            (d.DeviceModelId == deviceModelId ||
             (d.DeviceType == model.DeviceType &&
              d.Brand.ToLower() == brandNameLower &&
              d.Model.ToLower() == modelNameLower)));
    }

    public async Task<(List<DeviceModel> Items, int Total)> SearchAsync(
        DeviceType? deviceType,
        Guid? brandId,
        bool? isActive,
        string? search,
        int page,
        int pageSize)
    {
        var query = _context.DeviceModels
            .Include(m => m.Brand)
            .Where(m => m.DeletedAt == null && m.Brand.DeletedAt == null)
            .AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(m => m.IsActive == isActive.Value);
            if (isActive.Value)
            {
                query = query.Where(m => m.Brand.IsActive);
            }
        }

        if (deviceType.HasValue)
        {
            query = query.Where(m => m.DeviceType == deviceType.Value);
        }

        if (brandId.HasValue)
        {
            query = query.Where(m => m.BrandId == brandId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(m =>
                m.ModelName.ToLower().Contains(keyword) ||
                (m.ModelNumber != null && m.ModelNumber.ToLower().Contains(keyword)) ||
                m.Brand.Name.ToLower().Contains(keyword));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(m => m.Brand.Name)
            .ThenBy(m => m.ModelName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task AddAsync(DeviceModel model) => await _context.DeviceModels.AddAsync(model);

    public void Update(DeviceModel model) => _context.DeviceModels.Update(model);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
