using RepairShop.Domain.Modules.Identity;

namespace RepairShop.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user, string roleName);
}