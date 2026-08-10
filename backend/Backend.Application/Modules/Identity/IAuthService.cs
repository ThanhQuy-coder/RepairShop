using Backend.Application.Modules.Identity.DTOs;

namespace Backend.Application.Modules.Identity;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
}