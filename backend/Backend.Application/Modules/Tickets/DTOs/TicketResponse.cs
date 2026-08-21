namespace RepairShop.Application.Modules.Tickets.DTOs;

public record TicketResponse(
    Guid Id,
    string TicketCode,
    Guid CustomerId,
    Guid DeviceId,
    string Status,
    string IssueReported,
    string? Notes,
    string? ConditionNotes,
    string? RiskWarning,
    DateTime ReceivedAt);