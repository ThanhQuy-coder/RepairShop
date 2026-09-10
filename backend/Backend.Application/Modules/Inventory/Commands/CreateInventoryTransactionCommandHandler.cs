using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Modules.Inventory.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using RepairShop.Domain.Common.Exceptions;

namespace RepairShop.Application.Modules.Inventory.Commands;

public class CreateInventoryTransactionCommandHandler : IRequestHandler<CreateInventoryTransactionCommand, InventoryTransactionResponse>
{
    private readonly IPartRepository _partRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CreateInventoryTransactionCommandHandler> _logger;

    public CreateInventoryTransactionCommandHandler(
        IPartRepository partRepository, IInventoryRepository inventoryRepository,
        ICurrentUserService currentUser, ILogger<CreateInventoryTransactionCommandHandler> logger)
    {
        _partRepository = partRepository;
        _inventoryRepository = inventoryRepository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<InventoryTransactionResponse> Handle(CreateInventoryTransactionCommand request, CancellationToken cancellationToken)
    {
        var part = await _partRepository.GetByIdAsync(request.PartId)
            ?? throw new NotFoundException("Linh kiện", request.PartId);

        var inventory = await _inventoryRepository.GetByPartIdAsync(part.Id)
            ?? throw new NotFoundException("Tồn kho của linh kiện", request.PartId);

        var userId = _currentUser.UserId!.Value;

        // API Spec Tuần 2 chỉ cho phép "IMPORT"|"ADJUSTMENT" ở endpoint thủ công này
        // (EXPORT tự động qua UsePart() ở Repair Workflow, không đi qua API này — đúng BR-16)
        if (!Enum.TryParse<TransactionType>(request.Type, ignoreCase: true, out var type) || type == TransactionType.Export)
            throw new DomainException("Type chỉ được là 'Import' hoặc 'Adjustment' cho giao dịch thủ công.");

        var transaction = type == TransactionType.Import
            ? inventory.RecordImport(request.Quantity, userId)
            : inventory.RecordAdjustment(request.Quantity, isIncrease: true, userId); // Adjustment thủ công mặc định là tăng; điều chỉnh giảm dùng action riêng nếu cần mở rộng sau

        _inventoryRepository.TrackNewTransaction(transaction);
        await _inventoryRepository.SaveChangesAsync();

        _logger.LogInformation("Ghi nhận {Type} {Quantity} cho Part {PartId}, tồn mới: {NewQuantity}",
            type, request.Quantity, part.Id, inventory.QuantityOnHand);

        return new InventoryTransactionResponse(transaction.Id, part.Id, type.ToString(),
            request.Quantity, inventory.QuantityOnHand, transaction.CreatedAt);
    }
}