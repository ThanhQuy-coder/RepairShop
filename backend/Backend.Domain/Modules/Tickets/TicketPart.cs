using RepairShop.Domain.Common.Exceptions;

namespace RepairShop.Domain.Modules.Tickets;

public class TicketPart
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid RepairTicketId { get; private set; }
    public Guid PartId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPriceAtUse { get; private set; } // snapshot giá — đúng ghi chú Data Dictionary Tuần 2

    public decimal Subtotal => Quantity * UnitPriceAtUse;

    private TicketPart() { } // for EF Core

    internal TicketPart(Guid repairTicketId, Guid partId, int quantity, decimal unitPriceAtUse)
    {
        if (quantity <= 0)
            throw new DomainException("Số lượng linh kiện sử dụng phải lớn hơn 0.");

        RepairTicketId = repairTicketId;
        PartId = partId;
        Quantity = quantity;
        UnitPriceAtUse = unitPriceAtUse;
    }
}