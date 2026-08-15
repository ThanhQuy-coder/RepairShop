using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Modules.Identity;
using Microsoft.EntityFrameworkCore;

namespace RepairShop.Infrastructure.Persistence.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly AppDbContext _context;

    public RoleRepository(AppDbContext context) => _context = context;

    public Task<Role?> GetByNameAsync(string name) =>
        _context.Roles.FirstOrDefaultAsync(r => r.Name == name);
}