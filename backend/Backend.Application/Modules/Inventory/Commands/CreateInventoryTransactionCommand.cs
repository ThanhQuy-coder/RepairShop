using MediatR;

namespace RepairShop.Application.Modules.Inventory.Commands;

// Khớp API Spec Tuần 2: POST /api/inventory/transactions { partId, type, quantity }
public record CreateInventoryTransactionCommand(Guid PartId, string Type, int Quantity)
    : IRequest<InventoryTransactionResponse>;

public record InventoryTransactionResponse(Guid Id, Guid PartId, string Type, int Quantity, int NewQuantityOnHand, DateTime CreatedAt);