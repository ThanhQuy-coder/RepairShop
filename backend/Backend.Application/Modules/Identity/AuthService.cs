using Backend.Application.Common.Exceptions;
using Backend.Application.Common.Interfaces;
using Backend.Application.Modules.Identity.DTOs;
using Backend.Domain.Modules.Identity;

namespace Backend.Application.Modules.Identity;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    private const string DefaultSelfRegisterRole = "Customer";

    public AuthService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Kiểm tra Email tồn tại
        var existing = await _userRepository.GetByEmailAsync(request.Email);
        if (existing is not null)
            throw new EmailAlreadyExistsException(request.Email);

        // Gán role mặc định khi đăng ký
        var customerRole = await _roleRepository.GetByNameAsync(DefaultSelfRegisterRole)
            ?? throw new InvalidOperationException("Role 'Customer' chưa được seed trong database.");

        // Băm mật khẩu
        var passwordHash = _passwordHasher.Hash(request.Password);

        // Thêm User vào DB, sinh Token
        var user = new User(request.FullName, request.Email, passwordHash, customerRole.Id, request.Phone);

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, customerRole.Name);
        return new AuthResponse(accessToken, 3600, customerRole.Name, user.Email);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // Kiểm tra mật khẩu, Active
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        if (!user.IsActive)
            throw new InvalidCredentialsException();

        // Tạo role và lưu vào token
        var roleName = user.Role?.Name ?? DefaultSelfRegisterRole;
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, roleName);

        return new AuthResponse(accessToken, 3600, roleName, user.Email);
    }
}