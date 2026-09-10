using Microsoft.EntityFrameworkCore;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Modules.Inventory;
using RepairShop.Infrastructure.Persistence;

public class PartRepository : IPartRepository
{
    private readonly AppDbContext _context;
    public PartRepository(AppDbContext context) => _context = context;

    public Task<Part?> GetByIdAsync(Guid id)
    => _context.Parts.FirstOrDefaultAsync(p => p.Id == id);

    public Task<Part?> GetBySkuAsync(string sku)
    => _context.Parts.FirstOrDefaultAsync(p => p.Sku == sku);

    public async Task<(List<Part> Items, int Total)> SearchAsync(string? search,
        string? category, int page, int pageSize)
    {
        var query = _context.Parts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || p.Sku.Contains(search));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);

        var total = await query.CountAsync();
        var items = await query.OrderBy(p => p.Name).Skip((page - 1) * pageSize)
            .Take(pageSize).ToListAsync();

        return (items, total);
    }

    public async Task<List<Part>> ListAsync(string? search)
    {
        var query = _context.Parts.Where(p => p.IsActive).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || p.Sku.Contains(search));
        return await query.OrderBy(p => p.Name).Take(50).ToListAsync();
    }

    public async Task AddAsync(Part part) => await _context.Parts.AddAsync(part);

    public void Update(Part part) => _context.Parts.Update(part);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}