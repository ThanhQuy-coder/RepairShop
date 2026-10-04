namespace RepairShop.Application.Common.Interfaces;

public interface INotificationService
{
    Task CreateAsync(Guid userId, string type, string title, string message,
        string? relatedEntityType = null, Guid? relatedEntityId = null);

    /// <summary>Tạo thông báo cho TẤT CẢ user thuộc 1 role (VD: mọi Receptionist khi có lịch hẹn mới).</summary>
    Task CreateForRoleAsync(string roleName, string type, string title, string message,
        string? relatedEntityType = null, Guid? relatedEntityId = null);
}