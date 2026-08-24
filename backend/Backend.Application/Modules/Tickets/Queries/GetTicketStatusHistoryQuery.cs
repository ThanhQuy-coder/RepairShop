using MediatR;

public record GetTicketStatusHistoryQuery(Guid TicketId) 
    : IRequest<List<StatusHistoryResponse>>;
