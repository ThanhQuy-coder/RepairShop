using RepairShop.Application.Modules.Reports.Queries;

namespace RepairShop.Application.Common.Interfaces;

public interface IReportsQueryService
{
    Task<RepairSummary> GetRepairSummaryAsync();
    Task<RevenueSummary> GetRevenueSummaryAsync();
    Task<List<TechnicianSummaryItem>> GetTechnicianSummaryAsync();

    Task<RevenueReportResponse> GetRevenueReportAsync(DateTime? fromDate,
        DateTime? toDate, RevenueGroupBy groupBy);

    Task<List<TechnicianSummaryItem>> GetTechnicianPerformanceAsync(DateTime? fromDate, DateTime? toDate);
    Task<List<StatusBreakdownItem>> GetStatusBreakdownAsync();
    Task<int> GetTotalCustomersAsync();
}
