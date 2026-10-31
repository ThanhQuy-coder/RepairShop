using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepairShop.Domain.Modules.Inspections;

public sealed class InspectionTemplateConfiguration : IEntityTypeConfiguration<InspectionTemplate>
{
    public void Configure(EntityTypeBuilder<InspectionTemplate> builder)
    {
        builder.ToTable("InspectionTemplates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(255).IsRequired();
        builder.Property(x => x.DeviceType).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.InspectionType).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(x => new { x.DeviceType, x.InspectionType, x.SortOrder });
    }
}

public sealed class InspectionResultConfiguration : IEntityTypeConfiguration<InspectionResult>
{
    public void Configure(EntityTypeBuilder<InspectionResult> builder)
    {
        builder.ToTable("InspectionResults");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.InspectionType).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(x => new { x.TicketId, x.InspectionType }).IsUnique();
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.InspectionResultId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class InspectionItemResultConfiguration : IEntityTypeConfiguration<InspectionItemResult>
{
    public void Configure(EntityTypeBuilder<InspectionItemResult> builder)
    {
        builder.ToTable("InspectionItemResults");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ItemNameSnapshot).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(2000);
    }
}
