using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace RepairShop.Infrastructure.Persistence.Seeders;

public class ApplicationDbContextInitialiser
{
    private readonly AppDbContext _context;
    private readonly IEnumerable<IDataSeeder> _seeders;
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;

    public ApplicationDbContextInitialiser(
        AppDbContext context,
        IEnumerable<IDataSeeder> seeders,
        ILogger<ApplicationDbContextInitialiser> logger)
    {
        _context = context;
        _seeders = seeders;
        _logger = logger;
    }

    /// <summary>Áp dụng toàn bộ migration còn pending.</summary>
    public async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi chạy migration cho cơ sở dữ liệu.");
            throw;
        }
    }

    /// <summary>Chạy lần lượt các seeder đã đăng ký, theo thứ tự <see cref="IDataSeeder.Order"/>.</summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var seeder in _seeders.OrderBy(s => s.Order))
            {
                _logger.LogInformation("Đang chạy seeder {Seeder}", seeder.GetType().Name);
                await seeder.SeedAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi seed dữ liệu mồi.");
            throw;
        }
    }
}