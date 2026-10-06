using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Common;

namespace RepairShop.Application.Modules.Tickets;

/// <summary>
/// Gom logic "thông báo khi ticket đổi trạng thái" vào 1 chỗ, tránh lặp đoạn map status->message
/// ở từng Command Handler. KHÔNG đặt trong Domain vì Domain không được phụ thuộc INotificationService
/// (vi phạm nguyên tắc phụ thuộc 1 chiều) — đây là orchestration thuộc Application layer.
/// </summary>
public static class TicketNotificationHelper
{
    private static readonly Dictionary<string, string> StatusLabels = new()
    {
        [RepairStatusCodes.ReadyForPickup] = "Sẵn sàng bàn giao",
        [RepairStatusCodes.Delivered] = "Đã bàn giao",
    };

    public static async Task NotifyCustomerIfLinkedAsync(
        ICustomerRepository customerRepo, INotificationService notificationService,
        Guid customerId, string type, string title, string message, Guid ticketId)
    {
        var customer = await customerRepo.GetByIdAsync(customerId);
        if (customer?.UserId is null) return; // khách vãng lai không có tài khoản — bỏ qua, không lỗi

        await notificationService.CreateAsync(customer.UserId.Value, type, title, message, "Ticket", ticketId);
    }

    public static string LabelFor(string statusCode) => StatusLabels.GetValueOrDefault(statusCode, statusCode);

    internal static async Task NotifyCustomerIfLinkedAsync(object customerRepository, object notificationService, Guid customerId, string v1, string v2, string v3, Guid id)
    {
        throw new NotImplementedException();
    }
}