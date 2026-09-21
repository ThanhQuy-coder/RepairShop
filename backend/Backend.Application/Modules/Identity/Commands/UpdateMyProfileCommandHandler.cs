using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Identity.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Identity.Commands;

public class UpdateMyProfileCommandHandler : IRequestHandler<UpdateMyProfileCommand, UserProfileResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateMyProfileCommandHandler(IUserRepository userRepository, ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<UserProfileResponse> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("Không xác định được người dùng từ token.");
        var user = await _userRepository.GetByIdAsync(userId) ?? throw new NotFoundException("Người dùng", userId);

        user.UpdateProfile(request.FullName, request.Phone);
        await _userRepository.SaveChangesAsync();

        return new UserProfileResponse(user.Id, user.FullName, user.Email, user.Phone, user.Role?.Name ?? "Unknown", user.IsActive);
    }
}
