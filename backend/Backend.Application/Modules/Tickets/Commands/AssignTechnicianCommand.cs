using RepairShop.Application.Modules.Tickets.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Tickets.Commands;

public record AssignTechnicianCommand(Guid TicketId, Guid TechnicianId, string? Note) : IRequest<TicketResponse>;