using MediatR;

public record QuoteItemInput(string ItemType, string Description, int Quantity, decimal UnitPrice, Guid? PartId);

public record CreateQuoteCommand(Guid TicketId, string Description, List<QuoteItemInput> Items)
    : IRequest<QuoteResponse>;