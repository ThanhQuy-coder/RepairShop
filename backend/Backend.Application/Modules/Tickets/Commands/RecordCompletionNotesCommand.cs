using MediatR;

public record RecordCompletionNotesCommand(Guid TicketId, string CompletionNotes) : IRequest<Unit>;
