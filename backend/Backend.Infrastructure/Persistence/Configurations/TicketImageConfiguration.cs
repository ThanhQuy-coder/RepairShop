using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepairShop.Domain.Modules.Tickets;

public class TicketImageConfiguration : IEntityTypeConfiguration<TicketImage>
{
    public void Configure(EntityTypeBuilder<TicketImage> builder)
    {
        builder.ToTable("TicketImages");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.ImageUrl).HasMaxLength(500).IsRequired();
        builder.Property(i => i.ImageType).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Caption).HasMaxLength(255);
        builder.Property(i => i.UploadedAt).HasDefaultValueSql("now()");

        builder.HasOne<RepairShop.Domain.Modules.Identity.User>()
            .WithMany()
            .HasForeignKey(i => i.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}