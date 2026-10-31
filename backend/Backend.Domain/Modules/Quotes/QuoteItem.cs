using RepairShop.Domain.Common.Exceptions;
using RepairShop.Domain.Modules.Quotes.Enums;
using RepairShop.Domain.Modules.Content;
using RepairShop.Domain.Modules.Inventory;

namespace RepairShop.Domain.Modules.Quotes;

public class QuoteItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid QuoteId { get; private set; }
    public QuoteItemType ItemType { get; private set; }
    public Guid? PartId { get; private set; }
    public Guid? ServiceId { get; private set; }
    public Part? Part { get; private set; }
    public Service? Service { get; private set; }
    public string Description { get; private set; } = default!;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal Subtotal => Quantity * UnitPrice;

    private QuoteItem() { } // for EF Core

    internal QuoteItem(Guid quoteId, QuoteItemType itemType,
        string description, int quantity, decimal unitPrice, Guid? partId = null, Guid? serviceId = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Mô tả hạng mục báo giá không được để trống.");
        if (quantity <= 0)
            throw new DomainException("Số lượng phải lớn hơn 0.");
        if (unitPrice < 0)
            throw new DomainException("Đơn giá không thể âm.");
        if (itemType == QuoteItemType.Service && (serviceId is null || partId is not null))
            throw new DomainException("Hạng mục dịch vụ phải có ServiceId và không được có PartId.");
        if (itemType == QuoteItemType.Part && (partId is null || serviceId is not null))
            throw new DomainException("Hạng mục linh kiện phải có PartId và không được có ServiceId.");
        if (partId is not null && serviceId is not null)
            throw new DomainException("Một hạng mục không thể đồng thời liên kết dịch vụ và linh kiện.");

        QuoteId = quoteId;
        ItemType = itemType;
        PartId = partId;
        ServiceId = serviceId;
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}