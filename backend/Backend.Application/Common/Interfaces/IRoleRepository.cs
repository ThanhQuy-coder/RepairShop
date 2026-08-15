using RepairShop.Domain.Modules.Identity;

namespace RepairShop.Application.Common.Interfaces;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string name);
}