using MediatR;
using RepairShop.Application.Modules.Tickets.DTOs;

public record DeliverTicketCommand(Guid TicketId, string? DeliveryNote) : IRequest<TicketResponse>;
