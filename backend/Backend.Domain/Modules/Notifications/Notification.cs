namespace RepairShop.Domain.Modules.Notifications;

public class Notification
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string Type { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string Message { get; private set; } = default!;
    public string? RelatedEntityType { get; private set; }
    public Guid? RelatedEntityId { get; private set; }
    public bool IsRead { get; private set; } = false;
    public DateTime? ReadAt { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private Notification() { } // for EF Core

    public Notification(Guid userId, string type, string title, string message,
        string? relatedEntityType = null, Guid? relatedEntityId = null)
    {
        UserId = userId;
        Type = type;
        Title = title;
        Message = message;
        RelatedEntityType = relatedEntityType;
        RelatedEntityId = relatedEntityId;
    }

    public void MarkAsRead()
    {
        if (IsRead) return;
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }
}