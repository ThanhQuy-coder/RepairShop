using MediatR;

public record MarkInvoicePaidCommand(Guid InvoiceId, DateTime? PaidAt) : IRequest<InvoiceResponse>;
