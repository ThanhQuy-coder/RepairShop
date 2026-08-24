using MediatR;
using RepairShop.Application.Modules.Tickets.DTOs;

public record StartQualityCheckCommand(Guid TicketId) : IRequest<TicketResponse>;
