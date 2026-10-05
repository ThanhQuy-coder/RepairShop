namespace RepairShop.Application.Modules.Notifications.DTOs;

public record NotificationResponse(
    Guid Id, string Type, string Title, string Message,
    string? RelatedEntityType, Guid? RelatedEntityId, bool IsRead, DateTime? ReadAt, DateTime CreatedAt);

public record NotificationListResponse(List<NotificationResponse> Items, int Total, int UnreadCount);