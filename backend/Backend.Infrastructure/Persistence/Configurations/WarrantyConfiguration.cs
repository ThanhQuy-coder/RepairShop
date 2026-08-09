using Backend.Domain.Modules.Warranty;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

public class WarrantyConfiguration : IEntityTypeConfiguration<Warranty>
{
    public void Configure(EntityTypeBuilder<Warranty> builder)
    {
        builder.ToTable("Warranties");

        builder.HasKey(w => w.Id); // PK

        builder.Property(w => w.StartDate).HasColumnType("date").IsRequired();
        builder.Property(w => w.EndDate).HasColumnType("date").IsRequired();
        builder.Property(w => w.Terms).HasMaxLength(500);
        builder.Property(w => w.CreatedAt).HasDefaultValueSql("now()");

        // RepairTicketId là Unique — quan hệ 1-1 (1 ticket tối đa 1 Warranty, đúng BR-10)
        builder.HasIndex(w => w.RepairTicketId).IsUnique();

        // Quan hệ với RepairTicket đã khai đầy đủ (HasOne + Cascade) ở RepairTicketConfiguration
    }
}