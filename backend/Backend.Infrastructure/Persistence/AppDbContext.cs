using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSet<TEntity> sẽ được bổ sung dần ở Task tiếp theo (Xây dựng Entity, DbContext, Migration)
    // Giai đoạn này CHƯA khai báo entity nghiệp vụ — đúng tinh thần "dựng khung trước".

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tự động áp dụng toàn bộ IEntityTypeConfiguration<T> trong thư mục Configurations/
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}