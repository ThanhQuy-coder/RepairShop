using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Modules.Inventory.DTOs;
using MediatR;
using RepairShop.Application.Common.Interfaces;

namespace RepairShop.Application.Modules.Inventory.Queries;

public record GetPartByIdQuery(Guid Id) : IRequest<PartDetailResponse>;

public class GetPartByIdQueryHandler : IRequestHandler<GetPartByIdQuery, PartDetailResponse>
{
    private readonly IPartRepository _partRepository;
    private readonly IInventoryRepository _inventoryRepository;

    public GetPartByIdQueryHandler(IPartRepository partRepository, IInventoryRepository inventoryRepository)
    {
        _partRepository = partRepository;
        _inventoryRepository = inventoryRepository;
    }

    public async Task<PartDetailResponse> Handle(GetPartByIdQuery request, CancellationToken cancellationToken)
    {
        var part = await _partRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Linh kiện", request.Id);

        var inventory = await _inventoryRepository.GetByPartIdAsync(part.Id);

        return new PartDetailResponse(part.Id, part.Name, part.Sku, part.Category, part.CompatibleDeviceType,
            part.CostPrice, part.UnitPrice, part.Unit, part.MinStockThreshold,
            QuantityOnHand: inventory?.QuantityOnHand ?? 0,
            IsLowStock: inventory is not null && inventory.IsLowStock(part.MinStockThreshold),
            part.IsActive, part.CreatedAt);
    }
}