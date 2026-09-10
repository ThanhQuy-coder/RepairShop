using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Modules.Inventory;
using Microsoft.EntityFrameworkCore;

namespace RepairShop.Infrastructure.Persistence.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly AppDbContext _context;
    public InventoryRepository(AppDbContext context) => _context = context;

    public Task<Inventory?> GetByPartIdAsync(Guid partId) =>
        _context.Inventories.Include(i => i.Transactions).FirstOrDefaultAsync(i => i.PartId == partId);

    public async Task AddAsync(Inventory inventory) => await _context.Inventories.AddAsync(inventory);

    public void TrackNewTransaction(InventoryTransaction transaction) =>
        _context.Entry(transaction).State = EntityState.Added;

    public async Task<(List<InventoryTransactionView> Items, int Total)> SearchTransactionsAsync(
        Guid? partId, string? type, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        var query =
            from t in _context.InventoryTransactions
            join p in _context.Parts on t.PartId equals p.Id
            join u in _context.Users on t.PerformedByUserId equals u.Id
            select new { t, p.Name, u.FullName };

        if (partId is not null) query = query.Where(x => x.t.PartId == partId);
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(x => x.t.Type.ToString() == type);
        if (fromDate is not null) query = query.Where(x => x.t.CreatedAt >= fromDate);
        if (toDate is not null) query = query.Where(x => x.t.CreatedAt <= toDate);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.t.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new InventoryTransactionView(
                x.t.Id, x.t.PartId, x.Name, x.t.Type.ToString(), x.t.Quantity,
                x.t.RelatedTicketId, x.FullName, x.t.CreatedAt))
            .ToListAsync();

        return (items, total);
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}