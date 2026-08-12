using Backend.Application.Common.Exceptions;
using Backend.Application.Common.Interfaces;
using Backend.Application.Modules.Identity.DTOs;
using Backend.Domain.Common;
using Backend.Domain.Modules.Identity;
using MediatR;

namespace Backend.Application.Modules.Identity.Commands;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterCommandHandler(IUserRepository userRepository, IRoleRepository roleRepository,
        IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Lỗi FORMAT (email rỗng, sai định dạng...) đã bị chặn ở Validation Pipeline trước khi tới đây.
        // Ở đây chỉ còn lỗi NGHIỆP VỤ (email đã tồn tại) — 2 loại lỗi này tách biệt có chủ đích.
        var existing = await _userRepository.GetByEmailAsync(request.Email);
        if (existing is not null)
            throw new EmailAlreadyExistsException(request.Email);

        var customerRole = await _roleRepository.GetByNameAsync(Roles.Customer)
            ?? throw new InvalidOperationException("Role 'Customer' chưa được seed trong database.");

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = new User(request.FullName, request.Email, passwordHash, customerRole.Id, request.Phone);

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, customerRole.Name);
        return new AuthResponse(accessToken, 3600, customerRole.Name, user.Email);
    }
}