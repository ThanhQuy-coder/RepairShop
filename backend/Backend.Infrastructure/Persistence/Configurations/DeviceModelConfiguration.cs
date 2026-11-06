using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepairShop.Domain.Modules.Devices;

namespace RepairShop.Infrastructure.Persistence.Configurations;

public class DeviceModelConfiguration : IEntityTypeConfiguration<DeviceModel>
{
    public void Configure(EntityTypeBuilder<DeviceModel> builder)
    {
        builder.ToTable("DeviceModels");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.DeviceType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.ModelName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(m => m.ModelNumber)
            .HasMaxLength(100);

        builder.Property(m => m.IsActive)
            .HasDefaultValue(true);

        builder.Property(m => m.CreatedAt)
            .HasDefaultValueSql("now()");

        builder.HasIndex(m => m.DeviceType);
        builder.HasIndex(m => new { m.BrandId, m.DeviceType });
        builder.HasIndex(m => new { m.BrandId, m.DeviceType, m.ModelName })
            .IsUnique();

        builder.HasMany(m => m.Devices)
            .WithOne(d => d.DeviceModel)
            .HasForeignKey(d => d.DeviceModelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
