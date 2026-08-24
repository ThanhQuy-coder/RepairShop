using RepairShop.Domain.Modules.Warranty;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepairShop.Domain.Modules.Warranty.Enums;

namespace RepairShop.Infrastructure.Persistence.Configurations;

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
        builder.Property(w => w.WarrantyCode).HasMaxLength(20).IsRequired();
        builder.Property(w => w.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(WarrantyStatus.Active);

        // RepairTicketId là Unique — quan hệ 1-1 (1 ticket tối đa 1 Warranty, đúng BR-10)
        builder.HasIndex(w => w.RepairTicketId).IsUnique();
        builder.HasIndex(w => w.WarrantyCode).IsUnique();

        // Quan hệ với RepairTicket đã khai đầy đủ (HasOne + Cascade) ở RepairTicketConfiguration
    }
}