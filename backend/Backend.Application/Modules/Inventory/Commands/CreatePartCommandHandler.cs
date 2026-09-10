using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Inventory.DTOs;
using RepairShop.Domain.Modules.Inventory;
using MediatR;
using Microsoft.Extensions.Logging;

namespace RepairShop.Application.Modules.Inventory.Commands;

public class CreatePartCommandHandler : IRequestHandler<CreatePartCommand, PartDetailResponse>
{
    private readonly IPartRepository _partRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<CreatePartCommandHandler> _logger;

    public CreatePartCommandHandler(IPartRepository partRepository, IInventoryRepository inventoryRepository,
        ILogger<CreatePartCommandHandler> logger)
    {
        _partRepository = partRepository;
        _inventoryRepository = inventoryRepository;
        _logger = logger;
    }

    public async Task<PartDetailResponse> Handle(CreatePartCommand request, CancellationToken cancellationToken)
    {
        var existing = await _partRepository.GetBySkuAsync(request.Sku);
        if (existing is not null)
            throw new InvalidOperationException($"Linh kiện với SKU '{request.Sku}' đã tồn tại.");

        var part = new Part(request.Name, request.Sku, request.CostPrice, request.UnitPrice,
            request.Category, request.CompatibleDeviceType, request.Unit, request.MinStockThreshold);

        await _partRepository.AddAsync(part);
        await _partRepository.SaveChangesAsync();

        // Mỗi Part mới bắt buộc có đúng 1 dòng Inventory (BR-14: 1-1) — khởi tạo QuantityOnHand = 0,
        // Task 7.3 (nhập kho) sẽ là nơi đầu tiên cộng số lượng vào.
        var inventory = new Domain.Modules.Inventory.Inventory(part.Id);
        await _inventoryRepository.AddAsync(inventory);
        await _inventoryRepository.SaveChangesAsync();

        _logger.LogInformation("Tạo mới Part {PartId} - {Sku}", part.Id, part.Sku);

        return new PartDetailResponse(part.Id, part.Name, part.Sku, part.Category, part.CompatibleDeviceType,
            part.CostPrice, part.UnitPrice, part.Unit, part.MinStockThreshold,
            QuantityOnHand: 0, IsLowStock: part.MinStockThreshold > 0, part.IsActive, part.CreatedAt);
    }
}