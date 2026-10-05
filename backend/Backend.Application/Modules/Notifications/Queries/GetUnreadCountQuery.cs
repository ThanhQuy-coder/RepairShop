using RepairShop.Application.Common.Interfaces;
using MediatR;

namespace RepairShop.Application.Modules.Notifications.Queries;

public record UnreadCountResponse(int Count);
public record GetUnreadCountQuery : IRequest<UnreadCountResponse>;

public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, UnreadCountResponse>
{
    private readonly INotificationRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public GetUnreadCountQueryHandler(INotificationRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<UnreadCountResponse> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        var count = await _repo.GetUnreadCountAsync(_currentUser.UserId!.Value);
        return new UnreadCountResponse(count);
    }
}