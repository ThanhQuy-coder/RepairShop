using RepairShop.Domain.Modules.Appointments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RepairShop.Infrastructure.Persistence.Configurations;

public class TimeSlotConfigConfiguration : IEntityTypeConfiguration<TimeSlotConfig>
{
    public void Configure(EntityTypeBuilder<TimeSlotConfig> builder)
    {
        builder.ToTable("TimeSlotConfigs");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.SlotStart).IsRequired();
        builder.Property(t => t.SlotEnd).IsRequired();
        builder.Property(t => t.MaxCapacity).IsRequired();
        builder.Property(t => t.IsActive).HasDefaultValue(true);

        builder.HasIndex(t => new { t.DayOfWeek, t.IsActive });

        builder.HasData(
            new { Id = Guid.Parse("30000000-0000-0000-0000-000000000001"), DayOfWeek = (int?)null, SlotStart = new TimeOnly(9, 0), SlotEnd = new TimeOnly(10, 0), MaxCapacity = 5, IsActive = true },
            new { Id = Guid.Parse("30000000-0000-0000-0000-000000000002"), DayOfWeek = (int?)null, SlotStart = new TimeOnly(10, 0), SlotEnd = new TimeOnly(11, 0), MaxCapacity = 5, IsActive = true },
            new { Id = Guid.Parse("30000000-0000-0000-0000-000000000003"), DayOfWeek = (int?)null, SlotStart = new TimeOnly(14, 0), SlotEnd = new TimeOnly(15, 0), MaxCapacity = 5, IsActive = true },
            new { Id = Guid.Parse("30000000-0000-0000-0000-000000000004"), DayOfWeek = (int?)null, SlotStart = new TimeOnly(15, 0), SlotEnd = new TimeOnly(16, 0), MaxCapacity = 3, IsActive = true }
        );
    }
}