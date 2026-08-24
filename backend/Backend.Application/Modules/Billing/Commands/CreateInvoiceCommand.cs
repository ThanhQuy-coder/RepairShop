using MediatR;

public record CreateInvoiceCommand(Guid TicketId, string PaymentMethod) : IRequest<InvoiceResponse>;
