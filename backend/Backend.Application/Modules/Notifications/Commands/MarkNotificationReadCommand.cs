using RepairShop.Application.Common.Authorization;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using MediatR;

namespace RepairShop.Application.Modules.Notifications.Commands;

public record MarkNotificationReadCommand(Guid Id) : IRequest<Unit>;

public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Unit>
{
    private readonly INotificationRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public MarkNotificationReadCommandHandler(INotificationRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _repo.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Thông báo", request.Id);

        // BR-28 — enforce bằng Guard, KHÔNG dựa vào Role (Admin cũng không được đánh dấu hộ)
        NotificationAccessGuard.EnsureOwnsNotification(notification, _currentUser.UserId!.Value);

        notification.MarkAsRead();
        _repo.Update(notification);
        await _repo.SaveChangesAsync();

        return Unit.Value;
    }
}