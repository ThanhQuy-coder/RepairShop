using RepairShop.Application.Common.Exceptions;
using RepairShop.Domain.Modules.Notifications;

namespace RepairShop.Application.Common.Authorization;

/// <summary>
/// BR-28: User A chỉ được thao tác trên Notification của chính mình — không dựa vào Role
/// (Admin cũng KHÔNG được đọc/đánh dấu hộ Notification của người khác). Cùng nguyên tắc với
/// TicketAccessGuard/QuoteAccessGuard đã áp dụng từ Task 4.16/4.8.
/// </summary>
public static class NotificationAccessGuard
{
    public static void EnsureOwnsNotification(Notification notification, Guid currentUserId)
    {
        if (notification.UserId != currentUserId)
            throw new ForbiddenException("Bạn không có quyền truy cập thông báo này.");
    }
}