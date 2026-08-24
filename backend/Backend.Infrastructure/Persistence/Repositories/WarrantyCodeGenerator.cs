using RepairShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class WarrantyCodeGenerator : IWarrantyCodeGenerator
{
    private readonly AppDbContext _context;
    private const int MaxRetry = 5;

    public WarrantyCodeGenerator(AppDbContext context) => _context = context;

    public async Task<string> GenerateUniqueCodeAsync()
    {
        for (var attempt = 0; attempt < MaxRetry; attempt++)
        {
            var code = $"WR-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
            if (!await _context.Warranties.AnyAsync(w => w.WarrantyCode == code))
                return code;
        }
        return $"WR-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
    }
}