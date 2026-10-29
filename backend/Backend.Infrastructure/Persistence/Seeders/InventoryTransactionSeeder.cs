using Microsoft.EntityFrameworkCore;
using RepairShop.Domain.Modules.Inventory;
using RepairShop.Domain.Modules.Inventory.Enums;

namespace RepairShop.Infrastructure.Persistence.Seeders;

public class InventoryTransactionSeeder : IDataSeeder
{
    private static readonly Guid AdminUserId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly AppDbContext _context;

    public InventoryTransactionSeeder(AppDbContext context)
    {
        _context = context;
    }

    public int Order => 40;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var partIdsWithAdjustment = (await _context.Set<InventoryTransaction>()
            .Where(t => t.Type == TransactionType.Adjustment)
            .Select(t => t.PartId)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var inventories = await _context.Set<Inventory>()
            .Include(i => i.Transactions)
            .Where(i => !partIdsWithAdjustment.Contains(i.PartId))
            .ToListAsync(cancellationToken);

        if (inventories.Count == 0)
            return;

        foreach (var inventory in inventories)
        {
            var tx = inventory.RecordAdjustment(
                quantity: 5,
                isIncrease: true,
                performedByUserId: AdminUserId);

            _context.Set<InventoryTransaction>().Add(tx);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}