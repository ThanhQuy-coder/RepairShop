using MediatR;

namespace RepairShop.Application.Modules.Reports.Queries;

public record RepairSummary(int TotalTickets, int Pending, int InRepair, int Completed, int Cancelled);
public record RevenueSummary(decimal Today, decimal ThisWeek, decimal ThisMonth);
public record TechnicianSummaryItem(string TechnicianName, int Completed, int InProgress, double? AverageCompletionHours);
public record InventorySummary(int TotalParts, int LowStock, int OutOfStock);

public record DashboardSummaryResponse(
    RepairSummary Repair, RevenueSummary Revenue,
    List<TechnicianSummaryItem> Technicians, InventorySummary Inventory);

public record GetDashboardSummaryQuery : IRequest<DashboardSummaryResponse>;