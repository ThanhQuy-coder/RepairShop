using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepairShop.Domain.Modules.SLA;

namespace RepairShop.Infrastructure.Persistence.Configurations;

public sealed class SLAPolicyConfiguration : IEntityTypeConfiguration<SLAPolicy>
{
    public void Configure(EntityTypeBuilder<SLAPolicy> builder)
    {
        builder.ToTable("SLAPolicies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StatusCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.DurationMinutes).IsRequired();
        builder.HasIndex(x => new { x.StatusCode, x.DeviceType }).IsUnique();
    }
}
