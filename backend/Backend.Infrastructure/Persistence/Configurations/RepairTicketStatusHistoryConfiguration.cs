using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepairShop.Domain.Modules.Tickets;

public class RepairTicketStatusHistoryConfiguration : IEntityTypeConfiguration<RepairTicketStatusHistory>
{
    public void Configure(EntityTypeBuilder<RepairTicketStatusHistory> builder)
    {
        builder.ToTable("RepairTicketStatusHistories");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Note).HasMaxLength(255);
        builder.Property(h => h.ChangedAt).HasDefaultValueSql("now()");

        builder.HasOne(h => h.Status).WithMany().HasForeignKey(h => h.StatusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(h => h.ChangedByUser).WithMany().HasForeignKey(h => h.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => h.RepairTicketId); // hỗ trợ GET /tickets/{id}/status-history (API Spec Tuần 2)
    }
}