using RepairShop.Domain.Modules.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RepairShop.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id); // PK

        builder.Property(c => c.FullName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Phone).HasMaxLength(20).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(150);
        builder.Property(c => c.Address).HasMaxLength(255);
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(c => c.Phone).IsUnique();               // Unique
        builder.HasIndex(c => c.Email).IsUnique();                // Unique, nullable OK
        builder.HasIndex(c => c.UserId).IsUnique().HasFilter(null); // Unique 1 User <-> 1 Customer, filter cho phép nhiều NULL

        // FK: Customer -> User (0..1), optional
        builder.HasOne(c => c.User)
            .WithOne(u => u.Customer)
            .HasForeignKey<Customer>(c => c.UserId)
            .OnDelete(DeleteBehavior.SetNull); // User bị xoá -> Customer vẫn giữ, chỉ gỡ liên kết

        // Composition: Customer (1) -> Device (N)
        builder.HasMany(c => c.Devices)
            .WithOne(d => d.Customer)
            .HasForeignKey(d => d.CustomerId)
            .OnDelete(DeleteBehavior.Cascade); // Customer bị xóa -> Devices bị xóa

        // Association: Customer (1) -> RepairTicket (N)
        builder.HasMany(c => c.RepairTickets)
            .WithOne(t => t.Customer)
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.Restrict); // Restrict — chặn xoá Customer nếu còn ticket, buộc soft-delete thay vì hard-delete
    }
}