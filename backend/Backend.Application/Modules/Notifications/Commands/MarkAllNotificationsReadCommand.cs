using RepairShop.Application.Common.Interfaces;
using MediatR;

namespace RepairShop.Application.Modules.Notifications.Commands;

public record MarkAllNotificationsReadCommand : IRequest<Unit>;

public class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand, Unit>
{
    private readonly INotificationRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public MarkAllNotificationsReadCommandHandler(INotificationRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId!.Value;

        // GetUnreadByUserIdAsync() đã tự lọc theo đúng userId hiện tại — không cần Guard thêm
        // vì không có đường nào truyền userId của người khác vào đây (Command không nhận tham số userId).
        var unread = await _repo.GetUnreadByUserIdAsync(userId);
        foreach (var n in unread)
        {
            n.MarkAsRead();
            _repo.Update(n);
        }

        await _repo.SaveChangesAsync();
        return Unit.Value;
    }
}