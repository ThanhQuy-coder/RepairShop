using Backend.Domain.Modules.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

public class RepairStatusConfiguration : IEntityTypeConfiguration<RepairStatus>
{
    public void Configure(EntityTypeBuilder<RepairStatus> builder)
    {
        builder.ToTable("RepairStatuses");

        builder.HasKey(s => s.Id); // PK
        builder.Property(s => s.Id).ValueGeneratedNever(); // Id cố định theo danh sách seed (CHECKED_IN=1, ASSIGNED=2...)

        builder.Property(s => s.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(s => s.Code).IsUnique(); // Unique — code dùng để so sánh trong logic workflow (BR-19)

        builder.Property(s => s.Name).HasMaxLength(50).IsRequired();
        builder.Property(s => s.SortOrder).HasDefaultValue(0);
    }
}