using RepairShop.Application.Modules.Tickets.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Tickets.Commands;

public record CreateTicketCommand(
    Guid CustomerId,
    Guid DeviceId,
    string IssueDescription,
    string? Notes,
    string? ConditionNotes,
    string? RiskWarning) : IRequest<TicketResponse>;