using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Modules.Inventory.DTOs;
using MediatR;
using RepairShop.Application.Common.Interfaces;

namespace RepairShop.Application.Modules.Inventory.Commands;

public class UpdatePartCommandHandler : IRequestHandler<UpdatePartCommand, PartDetailResponse>
{
    private readonly IPartRepository _partRepository;
    private readonly IInventoryRepository _inventoryRepository;

    public UpdatePartCommandHandler(IPartRepository partRepository, IInventoryRepository inventoryRepository)
    {
        _partRepository = partRepository;
        _inventoryRepository = inventoryRepository;
    }

    public async Task<PartDetailResponse> Handle(UpdatePartCommand request, CancellationToken cancellationToken)
    {
        var part = await _partRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Linh kiện", request.Id);

        part.UpdateInfo(request.Name, request.CostPrice, request.UnitPrice,
            request.Category, request.CompatibleDeviceType, request.Unit, request.MinStockThreshold);

        _partRepository.Update(part);
        await _partRepository.SaveChangesAsync();

        var inventory = await _inventoryRepository.GetByPartIdAsync(part.Id);

        return new PartDetailResponse(part.Id, part.Name, part.Sku, part.Category, part.CompatibleDeviceType,
            part.CostPrice, part.UnitPrice, part.Unit, part.MinStockThreshold,
            QuantityOnHand: inventory?.QuantityOnHand ?? 0,
            IsLowStock: inventory?.IsLowStock(part.MinStockThreshold) ?? false,
            part.IsActive, part.CreatedAt);
    }
}