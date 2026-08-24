public record WarrantyResponse(string WarrantyCode, Guid TicketId, 
    DateOnly StartDate, DateOnly EndDate,
    string? Terms, string Status, bool IsExpired);