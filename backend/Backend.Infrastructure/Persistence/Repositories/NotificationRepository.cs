using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Modules.Notifications;
using Microsoft.EntityFrameworkCore;

namespace RepairShop.Infrastructure.Persistence.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;
    public NotificationRepository(AppDbContext context) => _context = context;

    public Task<Notification?> GetByIdAsync(Guid id) => _context.Notifications.FirstOrDefaultAsync(n => n.Id == id);

    public Task<List<Guid>> GetUserIdsByRoleAsync(string roleName) =>
        _context.Users.Where(u => u.Role.Name == roleName && u.IsActive).Select(u => u.Id).ToListAsync();

    public async Task<(List<Notification> Items, int Total, int UnreadCount)> GetByUserIdAsync(Guid userId, int page, int pageSize)
    {
        var query = _context.Notifications.Where(n => n.UserId == userId);

        var total = await query.CountAsync();
        var unreadCount = await query.CountAsync(n => !n.IsRead);

        // Mới nhất trước — đúng checklist "Pagination / sorting mới nhất trước"
        var items = await query.OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return (items, total, unreadCount);
    }

    public Task<int> GetUnreadCountAsync(Guid userId) =>
        _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public Task<List<Notification>> GetUnreadByUserIdAsync(Guid userId) =>
        _context.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();

    public async Task AddAsync(Notification notification) => await _context.Notifications.AddAsync(notification);
    public void Update(Notification notification) => _context.Notifications.Update(notification);
    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}