using MediatR;

public record QuoteItemInput(string ItemType, int Quantity, Guid? PartId, Guid? ServiceId);

public record CreateQuoteCommand(Guid TicketId, string Description, List<QuoteItemInput> Items)
    : IRequest<QuoteResponse>;