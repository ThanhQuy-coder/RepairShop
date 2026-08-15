using RepairShop.Domain.Modules.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RepairShop.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id); // PK
        builder.Property(r => r.Id).ValueGeneratedNever(); // int cố định (1=Admin, 2=Receptionist...), không auto-increment

        builder.Property(r => r.Name)
            .HasMaxLength(50)
            .IsRequired();
        builder.HasIndex(r => r.Name).IsUnique(); // Unique

        builder.Property(r => r.Description).HasMaxLength(255);

        builder.HasData(
            new Role(1, "Admin", "Quản trị viên"),
            new Role(2, "Receptionist", "Nhân viên tiếp nhận"),
            new Role(3, "Technician", "Kỹ thuật viên"),
            new Role(4, "Customer", "Khách hàng")
        );
    }
}