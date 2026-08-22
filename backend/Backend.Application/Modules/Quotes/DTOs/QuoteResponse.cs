public record QuoteResponse(Guid Id, Guid TicketId, string Description, decimal TotalAmount,
    string Status, List<QuoteItemResponse> Items, DateTime CreatedAt);