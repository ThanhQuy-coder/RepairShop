namespace RepairShop.Application.Modules.SLA.DTOs;

public sealed record SlaSummaryResponse(
    int OnTrack,
    int DueSoon,
    int Overdue,
    int ActiveTotal);
