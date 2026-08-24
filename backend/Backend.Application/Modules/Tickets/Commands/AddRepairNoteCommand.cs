using MediatR;

public record AddRepairNoteCommand(Guid TicketId, string Note) : IRequest<Unit>;