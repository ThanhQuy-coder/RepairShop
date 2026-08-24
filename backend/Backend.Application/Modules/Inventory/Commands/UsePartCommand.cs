using MediatR;

public record UsePartCommand(Guid TicketId, Guid PartId, int Quantity) : IRequest<UsePartResponse>;
