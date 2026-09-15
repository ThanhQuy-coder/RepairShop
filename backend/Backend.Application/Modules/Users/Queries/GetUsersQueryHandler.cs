using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Users.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Users.Queries;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, UserListResponse>
{
    private readonly IUserRepository _userRepository;

    public GetUsersQueryHandler(IUserRepository userRepository)
        => _userRepository = userRepository;

    public async Task<UserListResponse> Handle(GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var (items, total) = await _userRepository.SearchAsync(request.Role,
            request.IsActive, request.Page, request.PageSize);

        var mapped = items.Select(u => new UserListItemResponse(u.Id,
            u.FullName, u.Email, u.Role.Name, u.IsActive)).ToList();
        return new UserListResponse(mapped, total);
    }
}