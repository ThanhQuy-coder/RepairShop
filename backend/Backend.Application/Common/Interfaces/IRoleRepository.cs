using Backend.Domain.Modules.Identity;

namespace Backend.Application.Common.Interfaces;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string name);
}