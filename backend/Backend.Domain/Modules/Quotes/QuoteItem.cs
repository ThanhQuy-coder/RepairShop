using RepairShop.Domain.Common.Exceptions;
using RepairShop.Domain.Modules.Quotes.Enums;

namespace RepairShop.Domain.Modules.Quotes;

public class QuoteItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid QuoteId { get; private set; }
    public QuoteItemType ItemType { get; private set; }
    public Guid? PartId { get; private set; }
    public string Description { get; private set; } = default!;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal Subtotal => Quantity * UnitPrice;

    private QuoteItem() { } // for EF Core

    internal QuoteItem(Guid quoteId, QuoteItemType itemType,
        string description, int quantity, decimal unitPrice, Guid? partId = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Mô tả hạng mục báo giá không được để trống.");
        if (quantity <= 0)
            throw new DomainException("Số lượng phải lớn hơn 0.");
        if (unitPrice < 0)
            throw new DomainException("Đơn giá không thể âm.");

        QuoteId = quoteId;
        ItemType = itemType;
        PartId = partId;
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}