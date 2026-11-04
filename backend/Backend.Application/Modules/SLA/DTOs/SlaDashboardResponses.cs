using RepairShop.Domain.Common.Enums;

namespace RepairShop.Application.Modules.SLA.DTOs;

public sealed record SlaTicketItem(
    Guid TicketId,
    string TicketCode,
    string CustomerName,
    string DeviceLabel,
    string StatusCode,
    string StatusLabel,
    string? TechnicianName,
    DateTime StartedAt,
    DateTime DueAt,
    SLAStatus SlaStatus,
    bool ClockRunning);

public sealed record SlaTicketListResponse(
    IReadOnlyList<SlaTicketItem> Items,
    int Total);

public sealed record SlaStepSummary(
    string StatusCode,
    string Label,
    int Total,
    int Overdue);

public sealed record SlaTechnicianSummary(
    string TechnicianName,
    int InProgress,
    int DueSoon,
    int Overdue,
    decimal OnTimeRate);

public sealed record SlaDeviceSummary(
    string DeviceType,
    int Total,
    int Overdue);

public sealed record SlaAnalyticsResponse(
    int Total,
    int OnTrack,
    int DueSoon,
    int Overdue,
    decimal ComplianceRate,
    IReadOnlyList<SlaStepSummary> ByStatusStep,
    IReadOnlyList<SlaTechnicianSummary> ByTechnician,
    IReadOnlyList<SlaDeviceSummary> ByDeviceType);

public sealed record SlaCompletedItem(
    SlaTicketItem Ticket,
    bool CompletedOnTime);

public sealed record SlaMyTicketsResponse(
    int Overdue,
    int DueSoon,
    int OnTrack,
    IReadOnlyList<SlaTicketItem> Items,
    IReadOnlyList<SlaCompletedItem> RecentCompleted);
