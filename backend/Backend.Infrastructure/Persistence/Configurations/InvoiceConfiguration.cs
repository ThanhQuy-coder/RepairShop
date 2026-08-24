using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepairShop.Domain.Modules.Billing;
using RepairShop.Domain.Modules.Billing.Enums;
using RepairShop.Domain.Modules.Identity;
using RepairShop.Domain.Modules.Quotes;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.TotalAmount).HasColumnType("decimal(12,2)");
        builder.Property(i => i.PaymentMethod).HasConversion<string>().HasMaxLength(20).HasDefaultValue(PaymentMethod.Cash);
        builder.Property(i => i.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne<User>().WithMany().HasForeignKey(i => i.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Quote>().WithMany().HasForeignKey(i => i.QuoteId).OnDelete(DeleteBehavior.Restrict);
    }
}