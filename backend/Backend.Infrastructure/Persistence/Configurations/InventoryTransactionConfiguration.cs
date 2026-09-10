using RepairShop.Domain.Modules.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RepairShop.Infrastructure.Persistence.Configurations;

public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("InventoryTransactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne<Part>().WithMany().HasForeignKey(t => t.PartId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Domain.Modules.Identity.User>().WithMany()
            .HasForeignKey(t => t.PerformedByUserId).OnDelete(DeleteBehavior.Restrict);

        // BR-16: RelatedTicketId nullable — chỉ IMPORT/ADJUSTMENT được để null
        builder.Property(t => t.RelatedTicketId).IsRequired(false);

        builder.HasIndex(t => t.PartId);
        builder.HasIndex(t => t.RelatedTicketId);
        builder.HasIndex(t => t.CreatedAt); // hỗ trợ filter theo khoảng thời gian
    }
}