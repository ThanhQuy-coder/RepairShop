using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Inventory.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Inventory.Queries;

public class GetPartsListQueryHandler : IRequestHandler<GetPartsListQuery, PartListResponse>
{
    private readonly IPartRepository _partRepository;
    private readonly IInventoryRepository _inventoryRepository;

    public GetPartsListQueryHandler(IPartRepository partRepository, IInventoryRepository inventoryRepository)
    {
        _partRepository = partRepository;
        _inventoryRepository = inventoryRepository;
    }

    public async Task<PartListResponse> Handle(GetPartsListQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _partRepository.SearchAsync(request.Search, request.Category, request.Page, request.PageSize);

        var responses = new List<PartDetailResponse>();
        foreach (var part in items)
        {
            var inventory = await _inventoryRepository.GetByPartIdAsync(part.Id);
            responses.Add(new PartDetailResponse(
                part.Id, part.Name, part.Sku, part.Category, part.CompatibleDeviceType,
                part.CostPrice, part.UnitPrice, part.Unit, part.MinStockThreshold,
                QuantityOnHand: inventory?.QuantityOnHand ?? 0,
                IsLowStock: inventory is not null && inventory.IsLowStock(part.MinStockThreshold),
                part.IsActive, part.CreatedAt));
        }

        return new PartListResponse(responses, total);
    }
}