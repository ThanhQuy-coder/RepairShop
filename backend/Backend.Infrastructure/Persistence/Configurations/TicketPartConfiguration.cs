using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepairShop.Domain.Modules.Inventory;
using RepairShop.Domain.Modules.Tickets;

public class TicketPartConfiguration : IEntityTypeConfiguration<TicketPart>
{
    public void Configure(EntityTypeBuilder<TicketPart> builder)
    {
        builder.ToTable("TicketParts");
        builder.HasKey(tp => tp.Id);
        builder.Property(tp => tp.UnitPriceAtUse).HasColumnType("decimal(12,2)"); // snapshot giá — Data Dictionary Tuần 2

        builder.HasOne<Part>().WithMany().HasForeignKey(tp => tp.PartId).OnDelete(DeleteBehavior.Restrict);
    }
}