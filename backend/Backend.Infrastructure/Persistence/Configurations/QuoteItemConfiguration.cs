using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepairShop.Domain.Modules.Quotes;

public class QuoteItemConfiguration : IEntityTypeConfiguration<QuoteItem>
{
    public void Configure(EntityTypeBuilder<QuoteItem> builder)
    {
        builder.ToTable("QuoteItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.ItemType).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Description).HasMaxLength(255).IsRequired();
        builder.Property(i => i.UnitPrice).HasColumnType("decimal(12,2)");

        builder.HasOne<RepairShop.Domain.Modules.Inventory.Part>()
            .WithMany()
            .HasForeignKey(i => i.PartId)
            .OnDelete(DeleteBehavior.Restrict); // không xoá Part nếu còn nằm trong báo giá cũ
    }
}