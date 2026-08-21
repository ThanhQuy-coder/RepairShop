using MediatR;

namespace RepairShop.Application.Modules.Tickets.Commands;

public record UpdateIntakeConditionCommand(Guid TicketId, string? ConditionNotes, string? RiskWarning)
    : IRequest<Unit>;