using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepairShop.Domain.Modules.Diagnostics;

namespace RepairShop.Infrastructure.Persistence.Configurations;

public class DiagnosisSymptomConfiguration : IEntityTypeConfiguration<DiagnosisSymptom>
{
    public void Configure(EntityTypeBuilder<DiagnosisSymptom> builder)
    {
        builder.ToTable("DiagnosisSymptoms");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.DeviceType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.IsActive).HasDefaultValue(true);
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("now()");
        builder.HasIndex(s => new { s.DeviceType, s.IsActive, s.SortOrder });
    }
}
