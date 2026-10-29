using Microsoft.EntityFrameworkCore;
using RepairShop.Domain.Modules.Inventory;

namespace RepairShop.Infrastructure.Persistence.Seeders;

public class InventorySeeder : IDataSeeder
{
    private static readonly Guid AdminUserId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly AppDbContext _context;

    public InventorySeeder(AppDbContext context)
    {
        _context = context;
    }

    public int Order => 30;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var seededPartIds = (await _context.Set<Inventory>()
            .Select(i => i.PartId)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var parts = await _context.Set<Part>()
            .Where(p => !seededPartIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        if (parts.Count == 0)
            return;

        foreach (var part in parts)
        {
            var inventory = new Inventory(part.Id);

            inventory.RecordImport(
                quantity: GetInitialQuantity(part),
                performedByUserId: AdminUserId);

            _context.Set<Inventory>().Add(inventory);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static int GetInitialQuantity(Part part) => part.Category switch
    {
        "Màn hình" => 20,
        "Pin" => 30,
        "RAM" => 25,
        "Ổ cứng" => 25,
        "Sạc" => 40,
        "Cáp kết nối" => 60,
        _ => 50
    };
}