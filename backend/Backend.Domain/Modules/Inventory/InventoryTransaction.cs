using RepairShop.Domain.Common.Exceptions;
using RepairShop.Domain.Modules.Inventory.Enums;

namespace RepairShop.Domain.Modules.Inventory;

public class InventoryTransaction
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid PartId { get; private set; }
    public TransactionType Type { get; private set; }
    public int Quantity { get; private set; }
    public Guid? RelatedTicketId { get; private set; } // chỉ có giá trị khi Export tự động do dùng cho ticket (BR-16)
    public Guid PerformedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private InventoryTransaction() { } // for EF Core

    internal InventoryTransaction(Guid partId, TransactionType type, int quantity,
        Guid performedByUserId, Guid? relatedTicketId = null)
    {
        if (quantity <= 0)
            throw new DomainException("Số lượng giao dịch kho phải lớn hơn 0.");

        // BR-16: EXPORT do sửa chữa BẮT BUỘC có RelatedTicketId; IMPORT/ADJUSTMENT thì không bắt buộc.
        // Export THỦ CÔNG (Admin tự điều chỉnh, không qua ticket) dùng Type=Adjustment, không phải Export.
        if (type == TransactionType.Export && relatedTicketId is null)
            throw new DomainException("Giao dịch xuất kho do sửa chữa phải liên kết với 1 phiếu sửa chữa (RelatedTicketId).");

        PartId = partId;
        Type = type;
        Quantity = quantity;
        RelatedTicketId = relatedTicketId;
        PerformedByUserId = performedByUserId;
    }
}