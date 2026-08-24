using MediatR;

public record GetWarrantyByTicketQuery(Guid TicketId) : IRequest<WarrantyResponse>;
