using MediatR;

namespace RepairShop.Application.Modules.Reports.Queries;

public record RepairSummary(int TotalTickets, int Pending, int InRepair, int Completed, int Cancelled);
public record RevenueSummary(decimal Today, decimal ThisWeek, decimal ThisMonth);
public record TechnicianSummaryItem(string TechnicianName, int Completed, int InProgress, double? AverageCompletionHours);
public record InventorySummary(int TotalParts, int LowStock, int OutOfStock);
public record GetDashboardSummaryQuery : IRequest<DashboardSummaryResponse>;
public record StatusBreakdownItem(string StatusCode, string StatusLabel, int Count);
public record DashboardSummaryResponse(
    RepairSummary Repair, RevenueSummary Revenue,
    List<TechnicianSummaryItem> Technicians, InventorySummary Inventory,
    List<StatusBreakdownItem> StatusBreakdown,      // mới
    int TotalCustomers);                             // mới — "Customers" trong ô KPI mentor vẽ