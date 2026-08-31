using Microsoft.EntityFrameworkCore;
using RepairShop.Domain.Modules.Inventory;
using RepairShop.Infrastructure.Persistence;

public class PartRepository : IPartRepository
{
    private readonly AppDbContext _context;
    public PartRepository(AppDbContext context) => _context = context;

    public Task<Part?> GetByIdAsync(Guid id)
    {
        return _context.Parts.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Part>> ListAsync(string? search)
    {
        var query = _context.Parts.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || p.Sku.Contains(search));
        return await query.OrderBy(p => p.Name).Take(50).ToListAsync();
    }
}

public class InventoryRepository : IInventoryRepository
{
    private readonly AppDbContext _context;
    public InventoryRepository(AppDbContext context) => _context = context;

    public Task<Inventory?> GetByPartIdAsync(Guid partId)
    {
        return _context.Inventories.FirstOrDefaultAsync(i => i.PartId == partId);
    }
}