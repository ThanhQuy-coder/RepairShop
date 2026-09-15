using RepairShop.Domain.Modules.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RepairShop.Infrastructure.Persistence.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Services");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(150).IsRequired();
        builder.Property(s => s.BasePrice).HasColumnType("decimal(12,2)");
        builder.Property(s => s.DeviceType).HasMaxLength(20);
        builder.Property(s => s.IsActive).HasDefaultValue(true);
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("now()");
        builder.HasIndex(s => s.DeviceType);

        builder.HasData(
        new
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000101"),
            Name = "Thay pin điện thoại",
            Description = "Thay pin chính hãng/OEM cho các dòng điện thoại phổ biến, bảo hành pin 6 tháng.",
            BasePrice = (decimal?)350000,
            DeviceType = "Phone",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null,
            DeletedAt = (DateTime?)null,
        },
        new
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000102"),
            Name = "Thay màn hình điện thoại",
            Description = "Thay màn hình mới cho máy bị vỡ, sọc, cảm ứng liệt. Giá tùy dòng máy.",
            BasePrice = (decimal?)800000,
            DeviceType = "Phone",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null,
            DeletedAt = (DateTime?)null,
        },
        new
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000103"),
            Name = "Ép kính, thay mặt kính điện thoại",
            Description = "Ép lại kính nứt vỡ mà không cần thay nguyên cụm màn hình, tiết kiệm chi phí.",
            BasePrice = (decimal?)500000,
            DeviceType = "Phone",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 6, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null,
            DeletedAt = (DateTime?)null,
        },
        new
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000104"),
            Name = "Vệ sinh máy, tra keo tản nhiệt laptop",
            Description = "Vệ sinh bụi bẩn bên trong, tra keo tản nhiệt mới giúp máy mát hơn, giảm tiếng quạt.",
            BasePrice = (decimal?)250000,
            DeviceType = "Laptop",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 6, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null,
            DeletedAt = (DateTime?)null,
        },
        new
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000105"),
            Name = "Thay bàn phím laptop",
            Description = "Thay bàn phím mới cho máy bị liệt phím, vào nước, mất chữ.",
            BasePrice = (decimal?)450000,
            DeviceType = "Laptop",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 7, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null,
            DeletedAt = (DateTime?)null,
        },
        new
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000106"),
            Name = "Thay pin laptop",
            Description = "Thay pin chính hãng cho laptop bị chai pin, sụt dung lượng nhanh.",
            BasePrice = (decimal?)900000,
            DeviceType = "Laptop",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 7, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null,
            DeletedAt = (DateTime?)null,
        },
        new
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000107"),
            Name = "Kiểm tra và vệ sinh mainboard",
            Description = "Kiểm tra, vệ sinh mainboard cho thiết bị điện tử bị vào nước, chập cháy nhẹ.",
            BasePrice = (decimal?)200000,
            DeviceType = "Electronics",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null,
            DeletedAt = (DateTime?)null,
        },
        new
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000108"),
            Name = "Cài đặt phần mềm, xử lý virus",
            Description = "Cài lại hệ điều hành, phần mềm cần thiết, quét và diệt virus. Áp dụng cho mọi loại thiết bị.",
            BasePrice = (decimal?)150000,
            DeviceType = (string?)null,
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null,
            DeletedAt = (DateTime?)null,
        }
    );
    }
}