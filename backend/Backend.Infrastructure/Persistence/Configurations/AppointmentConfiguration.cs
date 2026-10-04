using RepairShop.Domain.Modules.Appointments;
using RepairShop.Domain.Modules.Customers;
using RepairShop.Domain.Modules.Identity;
using RepairShop.Domain.Modules.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RepairShop.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.FullName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Phone).HasMaxLength(20).IsRequired();
        builder.Property(a => a.DeviceType).HasMaxLength(20);
        builder.Property(a => a.Brand).HasMaxLength(50);
        builder.Property(a => a.Model).HasMaxLength(100);
        builder.Property(a => a.IssueDescription).HasMaxLength(500);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(
            RepairShop.Domain.Modules.Appointments.Enums.AppointmentStatus.Pending);
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne<Customer>().WithMany().HasForeignKey(a => a.CustomerId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<TimeSlotConfig>().WithMany().HasForeignKey(a => a.TimeSlotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(a => a.ConfirmedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RepairTicket>().WithOne().HasForeignKey<Appointment>(a => a.LinkedTicketId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => a.LinkedTicketId).IsUnique().HasFilter("\"LinkedTicketId\" IS NOT NULL"); // BR-22: 1 ticket <- tối đa 1 appointment
        builder.HasIndex(a => new { a.AppointmentDate, a.TimeSlotId });
        builder.HasIndex(a => a.Phone);
    }
}