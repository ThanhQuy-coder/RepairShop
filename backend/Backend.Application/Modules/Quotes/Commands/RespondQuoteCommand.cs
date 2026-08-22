using MediatR;

public record RespondQuoteCommand(Guid QuoteId, string Decision, string? RejectReason) : IRequest<QuoteResponse>;