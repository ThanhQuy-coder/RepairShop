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

    public Task<List<CatalogServiceItem>> GetAvailableServicesAsync(string deviceType)
    {
        // QUAN TRỌNG: entity Service (bảng dịch vụ + giá công khai) thuộc module Content,
        // theo đúng kế hoạch 8 tuần (Tuan_1.md, Task 1) chỉ được xây ở TUẦN 7 — hiện CHƯA TỒN TẠI
        // trong Domain. Trả về danh sách RỖNG có chủ đích ở đây, KHÔNG bịa dữ liệu Service giả
        // để "cho đủ demo" — đúng nguyên tắc Task 6.1 "không tự tạo dữ liệu không tồn tại".
        //
        // Hệ quả: context.availableServices luôn rỗng cho tới khi Service entity ra đời ở Tuần 7.
        // AI Service (FastAPI) đã tự xử lý đúng case này qua retrieval (Task 6.6): nếu
        // candidateServices rỗng nhưng candidateParts vẫn có dữ liệu, AI vẫn trả SUCCESS với
        // suggestedServices=[] và suggestedParts có giá trị — không coi là lỗi.
        //
        // TODO(Tuần 7): sau khi có Service entity thật, implement như GetAvailablePartsAsync
        // bên dưới — query theo deviceType, map sang CatalogServiceItem.
        return Task.FromResult(new List<CatalogServiceItem>());
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