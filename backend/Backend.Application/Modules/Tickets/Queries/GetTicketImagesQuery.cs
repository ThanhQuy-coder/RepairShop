using MediatR;
using RepairShop.Application.Modules.Tickets.Commands;

public record GetTicketImagesQuery(Guid TicketId) : IRequest<List<TicketImageResponse>>;
