using RepairShop.Application.Common.Interfaces;
using RepairShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace RepairShop.Infrastructure.ExternalServices;

/// <summary>
/// Format: RT-{yyyyMMdd}-{4 ký tự random}. Có kiểm tra trùng thật trong DB (retry tối đa 5 lần)
/// thay vì chỉ tin tưởng xác suất random không đụng nhau — đúng Acceptance Criteria "Ticket Code duy nhất".
/// </summary>
public class TicketCodeGenerator : ITicketCodeGenerator
{
    private readonly AppDbContext _context;
    private const int MaxRetry = 5;

    public TicketCodeGenerator(AppDbContext context) => _context = context;

    public async Task<string> GenerateUniqueCodeAsync()
    {
        for (var attempt = 0; attempt < MaxRetry; attempt++)
        {
            var code = $"RT-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

            var exists = await _context.RepairTickets.AnyAsync(t => t.TicketCode == code);
            if (!exists)
                return code;
        }

        // Cực hiếm khi xảy ra (5 lần liên tiếp trùng ngẫu nhiên) — fallback dùng thêm phần giây để chắc chắn unique
        return $"RT-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
    }
}