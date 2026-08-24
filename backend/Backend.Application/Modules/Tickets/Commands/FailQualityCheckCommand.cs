using MediatR;
using RepairShop.Application.Modules.Tickets.DTOs;

public record FailQualityCheckCommand(Guid TicketId, string FailureReason) : IRequest<TicketResponse>;

