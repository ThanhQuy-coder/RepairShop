using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepairShop.Domain.Modules.Warranty;

namespace RepairShop.Infrastructure.Persistence.Configurations;

public class WarrantyPackageConfiguration : IEntityTypeConfiguration<WarrantyPackage>
{
    public void Configure(EntityTypeBuilder<WarrantyPackage> builder)
    {
        builder.ToTable("WarrantyPackages");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.DurationMonths).IsRequired();
        builder.Property(p => p.Terms).HasMaxLength(500).IsRequired();
        builder.Property(p => p.IsActive).HasDefaultValue(true);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
        builder.HasIndex(p => p.Name).IsUnique();
        builder.HasIndex(p => p.DeletedAt);
        builder.HasData(
            new { Id = Guid.Parse("00000000-0000-0000-0000-000000000201"), Name = "Bảo hành cơ bản", Description = "Bảo hành lỗi kỹ thuật.", DurationMonths = 1, Terms = "Bảo hành lỗi kỹ thuật.", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null, DeletedAt = (DateTime?)null },
            new { Id = Guid.Parse("00000000-0000-0000-0000-000000000202"), Name = "Bảo hành linh kiện 3 tháng", Description = "Áp dụng cho linh kiện thay thế.", DurationMonths = 3, Terms = "Không bảo hành hư hỏng vật lý.", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null, DeletedAt = (DateTime?)null },
            new { Id = Guid.Parse("00000000-0000-0000-0000-000000000203"), Name = "Bảo hành pin 6 tháng", Description = "Áp dụng cho pin thay thế.", DurationMonths = 6, Terms = "Không bảo hành chai pin do sử dụng.", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null, DeletedAt = (DateTime?)null },
            new { Id = Guid.Parse("00000000-0000-0000-0000-000000000204"), Name = "Bảo hành linh kiện 12 tháng", Description = "Gói bảo hành dài hạn theo chính sách cửa hàng.", DurationMonths = 12, Terms = "Theo chính sách cửa hàng.", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null, DeletedAt = (DateTime?)null }
        );
    }
}
