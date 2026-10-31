using MediatR;

public record CreateWarrantyCommand(Guid TicketId, Guid WarrantyPackageId)
    : IRequest<WarrantyResponse>;
