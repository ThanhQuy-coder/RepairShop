using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Modules.Identity;
using Microsoft.EntityFrameworkCore;

namespace RepairShop.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    public Task<User?> GetByEmailAsync(string email) =>
        _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);

    public async Task AddAsync(User user) => await _context.Users.AddAsync(user);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();

    public Task<User?> GetByIdAsync(Guid id) =>
        _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);

    public async Task<(List<User> Items, int Total)> SearchAsync(string? roleName,
        bool? isActive, int page, int pageSize)
    {
        var query = _context.Users.Include(u => u.Role).AsQueryable();

        if (!string.IsNullOrWhiteSpace(roleName))
            query = query.Where(u => u.Role.Name == roleName);

        if (isActive is not null)
            query = query.Where(u => u.IsActive == isActive);

        var total = await query.CountAsync();
        var items = await query.OrderBy(u => u.FullName).Skip((page - 1) * pageSize)
            .Take(pageSize).ToListAsync();

        return (items, total);
    }
}