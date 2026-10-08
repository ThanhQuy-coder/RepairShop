using RepairShop.Application.Modules.Identity.DTOs;

namespace RepairShop.Application.Common.Interfaces;

public interface IRefreshTokenService
{
    Task<(AuthResponse Response, string RefreshToken)> IssueAsync(string email, CancellationToken cancellationToken = default);
    Task<(AuthResponse Response, string RefreshToken)> RotateAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);
}
