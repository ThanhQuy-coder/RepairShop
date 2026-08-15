using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Identity.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Identity.Queries;

public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, UserProfileResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetProfileQueryHandler(IUserRepository userRepository, ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<UserProfileResponse> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Không xác định được người dùng từ token.");

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        return new UserProfileResponse(user.Id, user.FullName, user.Email, user.Phone,
            user.Role?.Name ?? "Unknown", user.IsActive);
    }
}