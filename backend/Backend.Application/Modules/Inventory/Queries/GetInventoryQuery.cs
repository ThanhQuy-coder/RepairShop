using MediatR;
using RepairShop.Application.Common.Interfaces;

namespace RepairShop.Application.Modules.Inventory.Queries;

public record InventoryItemResponse(Guid PartId, string PartName, int QuantityOnHand, int MinStockThreshold, bool IsLowStock);
public record GetInventoryQuery : IRequest<List<InventoryItemResponse>>;

public class GetInventoryQueryHandler : IRequestHandler<GetInventoryQuery, List<InventoryItemResponse>>
{
    private readonly IPartRepository _partRepository;
    private readonly IInventoryRepository _inventoryRepository;

    public GetInventoryQueryHandler(IPartRepository partRepository, IInventoryRepository inventoryRepository)
    {
        _partRepository = partRepository;
        _inventoryRepository = inventoryRepository;
    }

    public async Task<List<InventoryItemResponse>> Handle(GetInventoryQuery request, CancellationToken cancellationToken)
    {
        var (parts, _) = await _partRepository.SearchAsync(null, null, 1, 500);
        var result = new List<InventoryItemResponse>();

        foreach (var part in parts)
        {
            var inventory = await _inventoryRepository.GetByPartIdAsync(part.Id);
            result.Add(new InventoryItemResponse(
                part.Id, part.Name, inventory?.QuantityOnHand ?? 0, part.MinStockThreshold,
                inventory is not null && inventory.IsLowStock(part.MinStockThreshold)));
        }

        return result;
    }
}