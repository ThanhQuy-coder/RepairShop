using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Identity.DTOs;
using RepairShop.Infrastructure.Persistence;

namespace RepairShop.Infrastructure.Identity;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenGenerator _jwt;
    private readonly JwtSettings _settings;

    public RefreshTokenService(AppDbContext db, IJwtTokenGenerator jwt, IOptions<JwtSettings> settings)
    {
        _db = db;
        _jwt = jwt;
        _settings = settings.Value;
    }

    public async Task<(AuthResponse Response, string RefreshToken)> IssueAsync(
        string email, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.Include(x => x.Role).SingleOrDefaultAsync(x => x.Email == email, cancellationToken)
            ?? throw new InvalidCredentialsException();
        return await CreateAsync(user, cancellationToken);
    }

    public async Task<(AuthResponse Response, string RefreshToken)> RotateAsync(
        string refreshToken, CancellationToken cancellationToken = default)
    {
        var hash = Hash(refreshToken);
        var stored = await _db.Set<Domain.Modules.Identity.RefreshToken>()
            .Include(x => x.User).ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (stored is null || !stored.IsActive || !stored.User.IsActive)
            throw new InvalidCredentialsException();

        stored.Revoke();
        return await CreateAsync(stored.User, cancellationToken);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var hash = Hash(refreshToken);
        var stored = await _db.Set<Domain.Modules.Identity.RefreshToken>()
            .SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (stored is null) return;
        stored.Revoke();
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<(AuthResponse Response, string RefreshToken)> CreateAsync(
        Domain.Modules.Identity.User user, CancellationToken cancellationToken)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var role = user.Role?.Name ?? "Customer";
        var expiresIn = _settings.ExpiryMinutes * 60;
        _db.Set<Domain.Modules.Identity.RefreshToken>().Add(
            new Domain.Modules.Identity.RefreshToken(user.Id, Hash(raw),
                DateTime.UtcNow.AddDays(_settings.RefreshTokenExpiryDays)));
        await _db.SaveChangesAsync(cancellationToken);
        return (new AuthResponse(_jwt.GenerateAccessToken(user, role), expiresIn, role, user.Email), raw);
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
