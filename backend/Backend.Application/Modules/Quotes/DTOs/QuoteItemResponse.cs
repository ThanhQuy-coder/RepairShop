public record QuoteItemResponse(Guid Id, string ItemType, Guid? PartId, Guid? ServiceId,
    string Description, int Quantity, decimal UnitPrice, decimal Subtotal);
