using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepairShop.Domain.Modules.SLA;

namespace RepairShop.Infrastructure.Persistence.Configurations;

public sealed class TicketSLAConfiguration : IEntityTypeConfiguration<TicketSLA>
{
    public void Configure(EntityTypeBuilder<TicketSLA> builder)
    {
        builder.ToTable("TicketSLAs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StatusCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => new { x.RepairTicketId, x.Status });
        builder.HasOne(x => x.RepairTicket).WithMany()
            .HasForeignKey(x => x.RepairTicketId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.SLAPolicy).WithMany()
            .HasForeignKey(x => x.SLAPolicyId).OnDelete(DeleteBehavior.Restrict);
    }
}
