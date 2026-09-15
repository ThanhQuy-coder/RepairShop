using RepairShop.Domain.Modules.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RepairShop.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id); // PK

        builder.Property(u => u.FullName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(150).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
        builder.Property(u => u.Phone).HasMaxLength(20);
        builder.Property(u => u.IsActive).HasDefaultValue(true);
        builder.Property(u => u.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Phone).IsUnique();

        // FK: User -> Role
        builder.HasOne(u => u.Role)
            .WithMany()
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => u.RoleId);

        builder.HasData(new
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            FullName = "Quản trị viên hệ thống",
            Email = "admin@repairshop.local",
            PasswordHash = "$2b$11$zCBDFaE.ComYvdPfXsGZDesI3XTP7NdwwfdM321U3pv1tMUuD0Gwy",
            Phone = (string?)null,
            RoleId = 1,
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null,
            DeletedAt = (DateTime?)null,
        });
    }
}