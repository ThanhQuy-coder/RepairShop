using RepairShop.Application.Common.Interfaces;
using MediatR;

namespace RepairShop.Application.Modules.Inventory.Queries;

public record InventoryDashboardResponse(int TotalParts, int LowStockCount, int OutOfStockCount);

public record GetInventoryDashboardQuery : IRequest<InventoryDashboardResponse>;

public class GetInventoryDashboardQueryHandler : IRequestHandler<GetInventoryDashboardQuery, InventoryDashboardResponse>
{
    private readonly IPartRepository _partRepository;
    private readonly IInventoryRepository _inventoryRepository;

    public GetInventoryDashboardQueryHandler(IPartRepository partRepository, IInventoryRepository inventoryRepository)
    {
        _partRepository = partRepository;
        _inventoryRepository = inventoryRepository;
    }

    public async Task<InventoryDashboardResponse> Handle(GetInventoryDashboardQuery request, CancellationToken cancellationToken)
    {
        var (parts, total) = await _partRepository.SearchAsync(null, null, 1, 1000);

        var lowStock = 0;
        var outOfStock = 0;

        foreach (var part in parts)
        {
            var inventory = await _inventoryRepository.GetByPartIdAsync(part.Id);
            var quantity = inventory?.QuantityOnHand ?? 0;

            if (quantity == 0)
                outOfStock++;
            else if (inventory is not null && inventory.IsLowStock(part.MinStockThreshold))
                lowStock++;
        }

        return new InventoryDashboardResponse(total, lowStock, outOfStock);
    }
}