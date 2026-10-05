using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Notifications.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Notifications.Queries;

public record GetMyNotificationsQuery(int Page = 1, int PageSize = 20) : IRequest<NotificationListResponse>;

public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, NotificationListResponse>
{
    private readonly INotificationRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public GetMyNotificationsQueryHandler(INotificationRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<NotificationListResponse> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId!.Value;
        var (items, total, unreadCount) = await _repo.GetByUserIdAsync(userId, request.Page, request.PageSize);

        var mapped = items.Select(n => new NotificationResponse(
            n.Id, n.Type, n.Title, n.Message, n.RelatedEntityType, n.RelatedEntityId, n.IsRead, n.ReadAt, n.CreatedAt)).ToList();

        return new NotificationListResponse(mapped, total, unreadCount);
    }
}