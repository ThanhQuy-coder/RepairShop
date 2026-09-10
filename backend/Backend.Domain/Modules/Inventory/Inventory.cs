using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Exceptions;
using RepairShop.Domain.Modules.Inventory.Enums;

namespace RepairShop.Domain.Modules.Inventory;

public class Inventory : BaseEntity
{
    public Guid PartId { get; private set; }
    public int QuantityOnHand { get; private set; }

    private readonly List<InventoryTransaction> _transactions = new();
    public IReadOnlyCollection<InventoryTransaction> Transactions => _transactions.AsReadOnly();

    private Inventory() { } // for EF Core

    public Inventory(Guid partId)
    {
        PartId = partId;
        QuantityOnHand = 0;
    }

    public bool Deduct(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Số lượng xuất kho phải lớn hơn 0.");

        if (QuantityOnHand < quantity)
            return false;

        QuantityOnHand -= quantity;
        MarkUpdated();
        return true;
    }

    public void Add(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Số lượng nhập kho phải lớn hơn 0.");

        QuantityOnHand += quantity;
        MarkUpdated();
    }

    public bool IsLowStock(int minThreshold) => QuantityOnHand < minThreshold;

    public InventoryTransaction RecordImport(int quantity, Guid performedByUserId)
    {
        Add(quantity);
        var transaction = new InventoryTransaction(PartId, TransactionType.Import, quantity, performedByUserId);
        _transactions.Add(transaction);
        return transaction;
    }

    public InventoryTransaction RecordAdjustment(int quantity,
        bool isIncrease, Guid performedByUserId)
    {
        if (isIncrease)
        {
            Add(quantity);
        }
        else
        {
            if (!Deduct(quantity))
                throw new InsufficientStockException("linh kiện này", quantity, QuantityOnHand);
        }

        var transaction = new InventoryTransaction(PartId, TransactionType.Adjustment, quantity, performedByUserId);
        _transactions.Add(transaction);
        return transaction;
    }

    public InventoryTransaction? RecordAutoExport(int quantity,
        Guid ticketId, Guid performedByUserId)
    {
        if (!Deduct(quantity))
            return null; // gọi nơi khác (UsePart) xử lý InsufficientStockException, giữ nguyên hành vi cũ

        var transaction = new InventoryTransaction(PartId, TransactionType.Export, quantity, performedByUserId, ticketId);
        _transactions.Add(transaction);
        return transaction;
    }
}