using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepairShop.Domain.Modules.Inventory;

public class PartConfiguration : IEntityTypeConfiguration<Part>
{
    public void Configure(EntityTypeBuilder<Part> builder)
    {
        builder.ToTable("Parts");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Sku).HasMaxLength(50).IsRequired();
        builder.HasIndex(p => p.Sku).IsUnique();

        builder.Property(p => p.Category).HasMaxLength(100);
        builder.Property(p => p.CompatibleDeviceType).HasMaxLength(20);

        builder.Property(p => p.CostPrice).HasColumnType("decimal(12,2)");
        builder.Property(p => p.UnitPrice).HasColumnType("decimal(12,2)");
        builder.Property(p => p.Unit).HasMaxLength(20).HasDefaultValue("cái");
        builder.Property(p => p.IsActive).HasDefaultValue(true);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(p => p.Category);
    }
}