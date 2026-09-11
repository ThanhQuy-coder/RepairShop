using RepairShop.Application.Common.Interfaces;
using RepairShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace RepairShop.Infrastructure.Persistence.Repositories;

/// <summary>
/// Nơi DUY NHẤT trong luồng AI Advisory truy vấn trực tiếp PostgreSQL — đúng sequence đã
/// thiết kế Task 5 Tuần 2 (mục 2.5, bước 3): "API → PostgreSQL: query Service/Part → lấy
/// availableServices/availableParts". AIServiceClient (Task 6.12) gọi interface này để build
/// context, sau đó gửi qua FastAPI — FastAPI KHÔNG BAO GIỜ tự chạm PostgreSQL.
/// </summary>
public class ServiceCatalogQueryService : IServiceCatalogQueryService
{
    private readonly AppDbContext _context;

    public ServiceCatalogQueryService(AppDbContext context) => _context = context;

    public async Task<List<CatalogServiceItem>> GetAvailableServicesAsync(string deviceType)
    {
        // Task 7.11: Service entity giờ đã tồn tại — thay thế đoạn "trả rỗng có chủ đích" (Task 6.15)
        // bằng query THẬT. Đây chính là điểm mentor lưu ý trước Tuần 6: AI Advisory sẽ tự động
        // có dữ liệu Service đầy đủ ngay khi module Content hoàn thiện, không cần sửa gì ở AI Service.
        var services = await _context.Services
            .Where(s => s.IsActive && (s.DeviceType == null || s.DeviceType == deviceType))
            .AsNoTracking()
            .Take(100)
            .ToListAsync();

        return services.Select(s => new CatalogServiceItem(
            s.Id.ToString(), s.Name, s.DeviceType ?? deviceType, s.BasePrice ?? 0)).ToList();
    }

    public async Task<List<CatalogPartItem>> GetAvailablePartsAsync()
    {
        // Dữ liệu THẬT từ bảng Parts (Task 4.2, Tuần 4) — AsNoTracking vì đây là read-only,
        // không cần EF Core theo dõi thay đổi. Giới hạn 100 record: đủ cho quy mô cửa hàng
        // nhỏ-vừa (đúng NFR-003), tránh gửi context quá lớn sang FastAPI làm chậm prompt LLM
        // (khớp NFR-002: AI phải phản hồi trong 5 giây).
        var parts = await _context.Parts
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Take(100)
            .ToListAsync();

        return parts.Select(p => new CatalogPartItem(p.Id.ToString(), p.Name, p.UnitPrice)).ToList();
    }
}