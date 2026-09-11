using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Modules.Content;
using Microsoft.EntityFrameworkCore;

namespace RepairShop.Infrastructure.Persistence.Repositories;

public class ServiceRepository : IServiceRepository
{
    private readonly AppDbContext _context;
    public ServiceRepository(AppDbContext context) => _context = context;

    public Task<Service?> GetByIdAsync(Guid id) => _context.Services.FirstOrDefaultAsync(s => s.Id == id);

    public async Task<(List<Service> Items, int Total)> SearchAsync(bool? isActive, int page, int pageSize)
    {
        var query = _context.Services.AsQueryable();
        if (isActive is not null) query = query.Where(s => s.IsActive == isActive);

        var total = await query.CountAsync();
        var items = await query.OrderBy(s => s.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task AddAsync(Service service) => await _context.Services.AddAsync(service);
    public void Update(Service service) => _context.Services.Update(service);
    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}