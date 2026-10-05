using RepairShop.Domain.Modules.Notifications;

namespace RepairShop.Application.Common.Interfaces;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id);
    Task<List<Guid>> GetUserIdsByRoleAsync(string roleName);
    Task<(List<Notification> Items, int Total, int UnreadCount)> GetByUserIdAsync(Guid userId, int page, int pageSize);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<List<Notification>> GetUnreadByUserIdAsync(Guid userId); // dùng cho mark-all
    Task AddAsync(Notification notification);
    void Update(Notification notification);
    Task SaveChangesAsync();
}