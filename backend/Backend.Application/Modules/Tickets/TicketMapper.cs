using RepairShop.Application.Modules.Tickets.DTOs;
using RepairShop.Domain.Modules.Tickets;

namespace RepairShop.Application.Modules.Tickets;

internal static class TicketMapper
{
    public static TicketResponse ToResponse(RepairTicket ticket) => new(
        ticket.Id, ticket.TicketCode, ticket.CustomerId, ticket.DeviceId, ticket.Status.Code,
        ticket.IssueReported, ticket.Notes, ticket.ConditionNotes, ticket.RiskWarning, ticket.ReceivedAt);
}