public record StatusHistoryResponse(
    Guid TicketId, string? FromStatus, string ToStatus, string ChangedByName, 
    DateTime ChangedAt, string? Note);
