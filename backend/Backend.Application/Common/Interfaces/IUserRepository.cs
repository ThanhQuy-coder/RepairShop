using RepairShop.Domain.Modules.Identity;

namespace RepairShop.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByPhoneAsync(string phone);
    Task AddAsync(User user);
    Task<User?> GetByIdAsync(Guid id);
    Task SaveChangesAsync();
    Task<(List<User> Items, int Total)> SearchAsync(string? roleName,
        bool? isActive, int page, int pageSize);
}