using Backend.Domain.Common.Enums;
using Backend.Domain.Modules.Quotes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

public class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> builder)
    {
        builder.ToTable("Quotes");

        builder.HasKey(q => q.Id); // PK

        builder.Property(q => q.Description).IsRequired();
        builder.Property(q => q.TotalAmount).HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(q => q.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(QuoteStatus.Pending);
        builder.Property(q => q.RejectReason).HasMaxLength(255);
        builder.Property(q => q.CreatedAt).HasDefaultValueSql("now()");

        // RepairTicketId KHÔNG unique — 1 ticket có thể có nhiều Quote (re-quote sau khi bị từ chối)
        builder.HasIndex(q => q.RepairTicketId);

        // FK: Quote -> User (CreatedByUserId)
        builder.HasOne(q => q.CreatedByUser)
            .WithMany()
            .HasForeignKey(q => q.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict); // Không xoá User nếu còn Quote do họ tạo (giữ vết audit)

        // Lưu ý: quan hệ ngược Quote -> RepairTicket đã khai ở RepairTicketConfiguration (HasMany Quotes)
        // Không khai lại HasOne ở đây để tránh EF Core hiểu nhầm thành 2 relationship khác nhau.
    }
}