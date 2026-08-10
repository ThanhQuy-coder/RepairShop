using Backend.Domain.Modules.Identity;

namespace Backend.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user, string roleName);
}