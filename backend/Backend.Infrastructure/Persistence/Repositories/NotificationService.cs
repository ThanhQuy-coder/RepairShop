using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Modules.Notifications;
using Microsoft.Extensions.Logging;

namespace RepairShop.Infrastructure.Notifications;

/// <summary>
/// Business event → INotificationService → Notification → Database (đúng sơ đồ Task 10.9).
/// CreateAsync lưu notification trước, sau đó đẩy realtime qua SignalR. Database là nguồn dữ liệu
/// bền vững để polling/reload không làm mất thông báo khi client offline.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly INotificationPusher _pusher;
    private readonly ILogger<NotificationService> _logger;
    public NotificationService(
        INotificationRepository repository,
        INotificationPusher pusher,
        ILogger<NotificationService> logger)
    {
        _repository = repository;
        _pusher = pusher;
        _logger = logger;
    }

    public async Task CreateAsync(Guid userId, string type, string title, string message,
        string? relatedEntityType = null, Guid? relatedEntityId = null)
    {
        var notification = new Notification(userId, type, title, message, relatedEntityType, relatedEntityId);
        await _repository.AddAsync(notification);
        await _repository.SaveChangesAsync();

        try
        {
            await _pusher.PushToUserAsync(userId, new
            {
                notification.Id,
                notification.Type,
                notification.Title,
                notification.Message,
                notification.RelatedEntityType,
                notification.RelatedEntityId,
                notification.CreatedAt,
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể đẩy notification realtime tới user {UserId}.", userId);
        }
    }

    public async Task CreateForRoleAsync(string roleName, string type, string title, string message,
        string? relatedEntityType = null, Guid? relatedEntityId = null)
    {
        var userIds = await _repository.GetUserIdsByRoleAsync(roleName);
        foreach (var userId in userIds)
            await CreateAsync(userId, type, title, message, relatedEntityType, relatedEntityId);
    }
}