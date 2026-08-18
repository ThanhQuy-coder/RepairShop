using RepairShop.Domain.Common.Exceptions;

namespace RepairShop.Domain.Modules.Quotes;

public class QuoteItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid QuoteId { get; private set; }
    public string Description { get; private set; } = default!;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal Subtotal => Quantity * UnitPrice;

    private QuoteItem() { } // for EF Core

    internal QuoteItem(Guid quoteId, string description, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Mô tả hạng mục báo giá không được để trống.");
        if (quantity <= 0)
            throw new DomainException("Số lượng phải lớn hơn 0.");
        if (unitPrice < 0)
            throw new DomainException("Đơn giá không thể âm.");

        QuoteId = quoteId;
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}