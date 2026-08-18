using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Exceptions;

namespace RepairShop.Domain.Modules.Inventory;

public class Inventory : BaseEntity
{
    public Guid PartId { get; private set; }
    public int QuantityOnHand { get; private set; }

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
            return false; // BR-20 — chặn ngay tại đây, không cho trừ âm

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
}