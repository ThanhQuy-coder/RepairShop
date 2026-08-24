using MediatR;

public record CreateWarrantyCommand(Guid TicketId, int WarrantyMonths, string? Terms) 
    : IRequest<WarrantyResponse>;
