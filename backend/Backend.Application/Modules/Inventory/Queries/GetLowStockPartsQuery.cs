using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Inventory.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Inventory.Queries;

public enum StockFilter { All, LowStock, OutOfStock }

public record GetLowStockPartsQuery(StockFilter Filter) : IRequest<List<PartDetailResponse>>;

public class GetLowStockPartsQueryHandler : IRequestHandler<GetLowStockPartsQuery, List<PartDetailResponse>>
{
    private readonly IPartRepository _partRepository;
    private readonly IInventoryRepository _inventoryRepository;

    public GetLowStockPartsQueryHandler(IPartRepository partRepository, IInventoryRepository inventoryRepository)
    {
        _partRepository = partRepository;
        _inventoryRepository = inventoryRepository;
    }

    public async Task<List<PartDetailResponse>> Handle(GetLowStockPartsQuery request, CancellationToken cancellationToken)
    {
        var (parts, _) = await _partRepository.SearchAsync(null, null, 1, 1000);
        var result = new List<PartDetailResponse>();

        foreach (var part in parts)
        {
            var inventory = await _inventoryRepository.GetByPartIdAsync(part.Id);
            var quantity = inventory?.QuantityOnHand ?? 0;
            var isLow = inventory is not null && inventory.IsLowStock(part.MinStockThreshold);
            var isOut = quantity == 0;

            var include = request.Filter switch
            {
                StockFilter.LowStock => isLow && !isOut,
                StockFilter.OutOfStock => isOut,
                _ => true,
            };

            if (!include) continue;

            result.Add(new PartDetailResponse(part.Id, part.Name, part.Sku, part.Category, part.CompatibleDeviceType,
                part.CostPrice, part.UnitPrice, part.Unit, part.MinStockThreshold, quantity, isLow, part.IsActive, part.CreatedAt));
        }

        return result.OrderBy(p => p.QuantityOnHand).ToList(); // hết hàng/sắp hết lên đầu
    }
}