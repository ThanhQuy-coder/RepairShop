using Backend.Application.Common.Exceptions;
using Backend.Application.Common.Interfaces;
using Backend.Application.Modules.Identity.DTOs;
using MediatR;

namespace Backend.Application.Modules.Identity.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        if (!user.IsActive)
            throw new InvalidCredentialsException();

        var roleName = user.Role?.Name ?? "Customer";
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, roleName);

        return new AuthResponse(accessToken, 3600, roleName, user.Email);
    }
}