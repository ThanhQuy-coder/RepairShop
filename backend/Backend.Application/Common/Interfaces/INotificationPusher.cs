namespace RepairShop.Application.Common.Interfaces;

/// <summary>
/// Trừu tượng hoá việc đẩy real-time — Application (NotificationService) chỉ biết interface này,
/// không biết SignalR/Hub tồn tại, đúng nguyên tắc tách lớp đã áp dụng cho IAIService (Task 6.12).
/// </summary>
public interface INotificationPusher
{
    Task PushToUserAsync(Guid userId, object payload);
    Task PushToRoleAsync(string role, object payload);
}