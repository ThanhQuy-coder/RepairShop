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
}