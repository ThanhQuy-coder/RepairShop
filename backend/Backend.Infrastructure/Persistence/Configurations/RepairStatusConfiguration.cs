using RepairShop.Domain.Modules.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RepairShop.Infrastructure.Persistence.Configurations;

public class RepairStatusConfiguration : IEntityTypeConfiguration<RepairStatus>
{
    public void Configure(EntityTypeBuilder<RepairStatus> builder)
    {
        builder.ToTable("RepairStatuses");

        builder.HasKey(s => s.Id); // PK
        builder.Property(s => s.Id).ValueGeneratedNever(); // Id cố định theo danh sách seed (CHECKED_IN=1, ASSIGNED=2...)

        builder.Property(s => s.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(s => s.Code).IsUnique(); // Unique — code dùng để so sánh trong logic workflow (BR-19)

        builder.Property(s => s.Name).HasMaxLength(50).IsRequired();
        builder.Property(s => s.SortOrder).HasDefaultValue(0);

        // Seed Repair Statuses
        builder.HasData(
            new
            {
                Id = (int)TicketStatus.CheckedIn,
                Code = "CHECKED_IN",
                Name = "Đã tiếp nhận",
                SortOrder = 1
            },
            new
            {
                Id = (int)TicketStatus.Assigned,
                Code = "ASSIGNED",
                Name = "Đã phân công",
                SortOrder = 2
            },
            new
            {
                Id = (int)TicketStatus.Diagnosing,
                Code = "DIAGNOSING",
                Name = "Đang chẩn đoán",
                SortOrder = 3
            },
            new
            {
                Id = (int)TicketStatus.WaitingApproval,
                Code = "WAITING_APPROVAL",
                Name = "Chờ khách hàng duyệt",
                SortOrder = 4
            },
            new
            {
                Id = (int)TicketStatus.OnHold,
                Code = "ON_HOLD",
                Name = "Tạm dừng",
                SortOrder = 5
            },
            new
            {
                Id = (int)TicketStatus.WaitingParts,
                Code = "WAITING_PARTS",
                Name = "Chờ linh kiện",
                SortOrder = 6
            },
            new
            {
                Id = (int)TicketStatus.InRepair,
                Code = "IN_REPAIR",
                Name = "Đang sửa chữa",
                SortOrder = 7
            },
            new
            {
                Id = (int)TicketStatus.QaTesting,
                Code = "QA_TESTING",
                Name = "Đang kiểm tra chất lượng",
                SortOrder = 8
            },
            new
            {
                Id = (int)TicketStatus.ReadyForPickup,
                Code = "READY_FOR_PICKUP",
                Name = "Sẵn sàng giao khách",
                SortOrder = 9
            },
            new
            {
                Id = (int)TicketStatus.Delivered,
                Code = "DELIVERED",
                Name = "Đã giao khách",
                SortOrder = 10
            },
            new
            {
                Id = (int)TicketStatus.ClosedRejected,
                Code = "CLOSED_REJECTED",
                Name = "Đã đóng - từ chối sửa",
                SortOrder = 11
            }
        );
    }
}