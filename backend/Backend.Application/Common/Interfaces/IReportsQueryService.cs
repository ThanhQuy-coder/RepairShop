using RepairShop.Application.Modules.Reports.Queries;

namespace RepairShop.Application.Common.Interfaces;

public interface IReportsQueryService
{
    Task<RepairSummary> GetRepairSummaryAsync();
    Task<RevenueSummary> GetRevenueSummaryAsync();
    Task<List<TechnicianSummaryItem>> GetTechnicianSummaryAsync();
}