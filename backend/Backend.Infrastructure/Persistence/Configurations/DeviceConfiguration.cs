using Backend.Domain.Modules.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("Devices");

        builder.HasKey(d => d.Id); // PK

        builder.Property(d => d.DeviceType)
            .HasConversion<string>() // lưu enum dạng string ("Phone")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.Brand).HasMaxLength(50).IsRequired();
        builder.Property(d => d.Model).HasMaxLength(100).IsRequired();
        builder.Property(d => d.SerialNumber).HasMaxLength(100); // nullable — không IsRequired()
        builder.Property(d => d.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(d => d.SerialNumber).IsUnique(); // Unique, cho phép nhiều NULL

        // Index hỗ trợ "xem lịch sử sửa chữa của thiết bị" — query theo CustomerId rất thường xuyên
        builder.HasIndex(d => d.CustomerId);

        // Association: Device (1) -> RepairTicket (N) — không cascade, lý do giống Customer ở trên
        builder.HasMany(d => d.RepairTickets)
            .WithOne(t => t.Device)
            .HasForeignKey(t => t.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}