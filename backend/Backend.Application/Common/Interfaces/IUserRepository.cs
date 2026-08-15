using RepairShop.Domain.Modules.Identity;

namespace RepairShop.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task AddAsync(User user);
    Task<User?> GetByIdAsync(Guid id);
    Task SaveChangesAsync();
}