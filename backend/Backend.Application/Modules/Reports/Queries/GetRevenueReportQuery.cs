using MediatR;

namespace RepairShop.Application.Modules.Reports.Queries;

public enum RevenueGroupBy { Day, Month }

public record RevenuePeriodItem(string Period, decimal TotalRevenue, int TicketCount);
public record RevenueReportResponse(
    List<RevenuePeriodItem> Items, decimal TotalRevenue,
    int TotalInvoices, int PaidInvoices, int UnpaidInvoices);

public record GetRevenueReportQuery(DateTime? FromDate, DateTime? ToDate, RevenueGroupBy GroupBy)
    : IRequest<RevenueReportResponse>;

public record ProfitReportItem(
    string TicketCode,
    decimal Revenue,
    decimal Cost,
    decimal GrossProfit,
    decimal MarginPercent);

public record ProfitReportResponse(
    List<ProfitReportItem> Items,
    decimal TotalRevenue,
    decimal TotalCost,
    decimal GrossProfit,
    decimal MarginPercent);

public record GetProfitReportQuery(DateTime? FromDate, DateTime? ToDate)
    : IRequest<ProfitReportResponse>;