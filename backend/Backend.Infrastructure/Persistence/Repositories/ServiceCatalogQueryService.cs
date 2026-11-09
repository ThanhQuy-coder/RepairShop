using RepairShop.Application.Common.Interfaces;
using RepairShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace RepairShop.Infrastructure.Persistence.Repositories;

/// <summary>
/// Nơi DUY NHẤT trong luồng AI Advisory/MAS truy vấn trực tiếp PostgreSQL — đúng sequence
/// đã thiết kế Task 5 Tuần 2 (mục 2.5, bước 3). FastAPI KHÔNG BAO GIỜ tự chạm PostgreSQL.
///
/// Task 3.1 (MAS) sửa nợ D-06:
/// - Service: lấy thêm Description, tăng giới hạn lên 200 (context MAS).
/// - Part: lọc IsActive, lọc CompatibleDeviceType theo thiết bị, join Inventories để biết
///   còn hàng; trả cả CompatibleDeviceType/InStock/QuantityOnHand cho context MAS.
/// </summary>
public class ServiceCatalogQueryService : IServiceCatalogQueryService
{
    private const int LegacyServiceLimit = 100;
    private const int MasCatalogLimit = 200;

    private readonly AppDbContext _context;

    public ServiceCatalogQueryService(AppDbContext context) => _context = context;

    public async Task<List<CatalogServiceItem>> GetAvailableServicesAsync(string deviceType)
    {
        var services = await _context.Services
            .Where(s => s.IsActive && (s.DeviceType == null || s.DeviceType == deviceType))
            .OrderBy(s => s.Name)
            .AsNoTracking()
            .Take(MasCatalogLimit)
            .ToListAsync();

        return services
            .Select(s => new CatalogServiceItem(
                s.Id.ToString(),
                s.Name,
                s.DeviceType ?? deviceType,
                s.BasePrice ?? 0,
                s.Description))
            .ToList();
    }

    /// <summary>Giữ hành vi cũ cho luồng legacy /v1/advisory (không lọc theo deviceType).</summary>
    public Task<List<CatalogPartItem>> GetAvailablePartsAsync() =>
        QueryPartsAsync(deviceType: null, limit: LegacyServiceLimit);

    /// <summary>Dùng cho MAS: lọc đúng loại thiết bị + còn hàng, trả kèm tồn kho.</summary>
    public Task<List<CatalogPartItem>> GetAvailablePartsAsync(string? deviceType) =>
        QueryPartsAsync(deviceType, MasCatalogLimit);

    private async Task<List<CatalogPartItem>> QueryPartsAsync(string? deviceType, int limit)
    {
        // AsNoTracking vì đây là read-only, không cần EF Core theo dõi thay đổi.
        var query = _context.Parts
            .AsNoTracking()
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(deviceType))
        {
            var normalized = deviceType.Trim().ToLowerInvariant();
            // CompatibleDeviceType được seed dạng "Phone"/"Laptop" => so sánh không phân biệt hoa thường.
            query = query.Where(p =>
                p.CompatibleDeviceType == null ||
                p.CompatibleDeviceType.ToLower() == normalized);
        }

        var parts = await query
            .OrderBy(p => p.Name)
            .Take(limit)
            .ToListAsync();

        if (parts.Count == 0)
            return [];

        var partIds = parts.Select(p => p.Id).ToList();
        var stockByPart = await _context.Inventories
            .AsNoTracking()
            .Where(i => partIds.Contains(i.PartId))
            .ToDictionaryAsync(i => i.PartId, i => i.QuantityOnHand);

        return parts
            .Select(p =>
            {
                var quantity = stockByPart.TryGetValue(p.Id, out var value) ? value : 0;
                return new CatalogPartItem(
                    p.Id.ToString(),
                    p.Name,
                    p.UnitPrice,
                    p.CompatibleDeviceType,
                    InStock: quantity > 0,
                    QuantityOnHand: quantity);
            })
            .ToList();
    }
}
