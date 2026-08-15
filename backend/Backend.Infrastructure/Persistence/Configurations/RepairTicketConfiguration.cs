using RepairShop.Domain.Modules.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RepairShop.Infrastructure.Persistence.Configurations;

public class RepairTicketConfiguration : IEntityTypeConfiguration<RepairTicket>
{
    public void Configure(EntityTypeBuilder<RepairTicket> builder)
    {
        builder.ToTable("RepairTickets");

        builder.HasKey(t => t.Id); // PK

        builder.Property(t => t.TicketCode).HasMaxLength(20).IsRequired();
        builder.HasIndex(t => t.TicketCode).IsUnique(); // Unique — dùng để tra cứu công khai (FR-029, GET /tickets/track/{ticketCode})

        builder.Property(t => t.IssueReported).IsRequired(); // text, không giới hạn HasMaxLength
        builder.Property(t => t.DiagnosisResult);
        builder.Property(t => t.DiagnosticDeposit).HasColumnType("decimal(12,2)").HasDefaultValue(0);
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("now()");

        // FK: RepairTicket -> Device / Customer đã cấu hình bên phía Device/Customer config (tránh khai 2 lần 2 chiều)

        // FK: RepairTicket -> User (Receptionist) — bắt buộc, restrict
        builder.HasOne(t => t.Receptionist)
            .WithMany()
            .HasForeignKey(t => t.ReceptionistId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK: RepairTicket -> User (Technician), nullable — restrict
        builder.HasOne(t => t.Technician)
            .WithMany()
            .HasForeignKey(t => t.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK: RepairTicket -> RepairStatus
        builder.HasOne(t => t.Status)
            .WithMany()
            .HasForeignKey(t => t.StatusId)
            .OnDelete(DeleteBehavior.Restrict); // Không cho xoá RepairStatus nếu còn ticket đang dùng

        // Self-reference: ParentTicket (BR-12, ca bảo hành)
        builder.HasOne(t => t.ParentTicket)
            .WithMany(t => t.WarrantyTickets)
            .HasForeignKey(t => t.ParentTicketId)
            .OnDelete(DeleteBehavior.Restrict); // BẮT BUỘC Restrict

        // Index hỗ trợ các query thường xuyên: GET /api/tickets?status=&technicianId=&customerId=
        builder.HasIndex(t => t.StatusId);
        builder.HasIndex(t => t.TechnicianId);
        builder.HasIndex(t => t.CustomerId);
        builder.HasIndex(t => t.ParentTicketId);

        // Composition: RepairTicket (1) -> Quote (N)
        builder.HasMany(t => t.Quotes)
            .WithOne(q => q.RepairTicket)
            .HasForeignKey(q => q.RepairTicketId)
            .OnDelete(DeleteBehavior.Cascade); // Cascade — Quote không có ý nghĩa tồn tại độc lập nếu Ticket bị xoá

        // Composition: RepairTicket (1) -> Warranty (0..1)
        builder.HasOne(t => t.Warranty)
            .WithOne(w => w.RepairTicket)
            .HasForeignKey<RepairShop.Domain.Modules.Warranty.Warranty>(w => w.RepairTicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}