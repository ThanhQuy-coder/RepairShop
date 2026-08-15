using RepairShop.Application.Modules.Identity.DTOs;

namespace RepairShop.Application.Modules.Identity;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
}