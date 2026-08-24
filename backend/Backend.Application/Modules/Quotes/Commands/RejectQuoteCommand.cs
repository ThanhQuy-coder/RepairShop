using MediatR;

public record RejectQuoteCommand(Guid QuoteId, string RejectReason) : IRequest<QuoteResponse>;