using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepairShop.Domain.Modules.Inventory;

public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("Inventories");
        builder.HasKey(i => i.Id);
        builder.HasIndex(i => i.PartId).IsUnique(); // BR-14: 1-1 với Part
        builder.HasOne<Part>().WithOne().HasForeignKey<Inventory>(i => i.PartId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}