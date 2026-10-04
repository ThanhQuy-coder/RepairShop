using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Modules.Notifications;

namespace RepairShop.Infrastructure.Notifications;

/// <summary>
/// Business event → INotificationService → Notification → Database (đúng sơ đồ Task 10.9).
/// Chỉ có CreateAsync()/CreateForRoleAsync() — KHÔNG có logic đẩy real-time (SignalR để dành
/// Tuần sau), đây thuần túy là lớp ghi dữ liệu, đúng phạm vi "notification data layer" đã chốt.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    public NotificationService(INotificationRepository repository) => _repository = repository;

    public async Task CreateAsync(Guid userId, string type, string title, string message,
        string? relatedEntityType = null, Guid? relatedEntityId = null)
    {
        var notification = new Notification(userId, type, title, message, relatedEntityType, relatedEntityId);
        await _repository.AddAsync(notification);
        // KHÔNG SaveChangesAsync() ở đây — dùng chung transaction với nghiệp vụ gọi nó (VD CreateAppointment),
        // đảm bảo Notification và business event luôn cùng tồn tại hoặc cùng rollback.
    }

    public async Task CreateForRoleAsync(string roleName, string type, string title, string message,
        string? relatedEntityType = null, Guid? relatedEntityId = null)
    {
        var userIds = await _repository.GetUserIdsByRoleAsync(roleName);
        foreach (var userId in userIds)
            await CreateAsync(userId, type, title, message, relatedEntityType, relatedEntityId);
    }
}