using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Reports.Queries;
using RepairShop.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace RepairShop.Infrastructure.Persistence.Repositories;

public class ReportsQueryService : IReportsQueryService
{
    private readonly AppDbContext _context;
    public ReportsQueryService(AppDbContext context) => _context = context;

    // Nhóm trạng thái cho Repair Summary — khớp đúng gom nhóm đã dùng ở Frontend Dashboard (Task 5.15),
    // giờ tính THẬT ở tầng SQL thay vì kéo hết dữ liệu về đếm tay như giải pháp tạm trước đây.
    private static readonly string[] PendingCodes =
        [RepairStatusCodes.CheckedIn, RepairStatusCodes.Assigned, RepairStatusCodes.Diagnosing,
         RepairStatusCodes.WaitingApproval, RepairStatusCodes.OnHold];
    private static readonly string[] InRepairCodes =
        [RepairStatusCodes.WaitingParts, RepairStatusCodes.InRepair, RepairStatusCodes.QaTesting,
         RepairStatusCodes.ReadyForPickup];

    public async Task<RepairSummary> GetRepairSummaryAsync()
    {
        var total = await _context.RepairTickets.CountAsync();
        var pending = await _context.RepairTickets.CountAsync(t => PendingCodes.Contains(t.Status.Code));
        var inRepair = await _context.RepairTickets.CountAsync(t => InRepairCodes.Contains(t.Status.Code));
        var completed = await _context.RepairTickets.CountAsync(t => t.Status.Code == RepairStatusCodes.Delivered);
        var cancelled = await _context.RepairTickets.CountAsync(t => t.Status.Code == RepairStatusCodes.ClosedRejected);

        return new RepairSummary(total, pending, inRepair, completed, cancelled);
    }

    public async Task<RevenueSummary> GetRevenueSummaryAsync()
    {
        // Revenue = SUM(Invoice.TotalAmount) WHERE PaidAt IS NOT NULL — đúng lưu ý Task 7.7:
        // "Không lấy tất cả Invoice làm doanh thu, Invoice chưa thanh toán -> chưa tính revenue."
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var weekStart = now.Date.AddDays(-(int)now.DayOfWeek + (now.DayOfWeek == DayOfWeek.Sunday ? -6 : 1));
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var paidInvoices = _context.Invoices.Where(i => i.PaidAt != null);

        var today = await paidInvoices.Where(i => i.PaidAt >= todayStart).SumAsync(i => (decimal?)i.TotalAmount) ?? 0;
        var thisWeek = await paidInvoices.Where(i => i.PaidAt >= weekStart).SumAsync(i => (decimal?)i.TotalAmount) ?? 0;
        var thisMonth = await paidInvoices.Where(i => i.PaidAt >= monthStart).SumAsync(i => (decimal?)i.TotalAmount) ?? 0;

        return new RevenueSummary(today, thisWeek, thisMonth);
    }

    public async Task<List<TechnicianSummaryItem>> GetTechnicianSummaryAsync()
    {
        var technicians = await _context.Users
            .Where(u => u.Role.Name == Roles.Technician && u.IsActive)
            .ToListAsync();

        var result = new List<TechnicianSummaryItem>();

        foreach (var tech in technicians)
        {
            var completed = await _context.RepairTickets
                .CountAsync(t => t.TechnicianId == tech.Id && t.Status.Code == RepairStatusCodes.Delivered);

            var inProgress = await _context.RepairTickets
                .CountAsync(t => t.TechnicianId == tech.Id && InRepairCodes.Contains(t.Status.Code));

            // Average completion: từ ReceivedAt -> DeliveredAt, chỉ tính ticket đã DELIVERED —
            // KHÔNG xây thuật toán đánh giá nhân viên phức tạp (đúng lưu ý Task 7.8), chỉ 1 con số trung bình đơn giản.
            var deliveredTickets = await _context.RepairTickets
                .Where(t => t.TechnicianId == tech.Id && t.Status.Code == RepairStatusCodes.Delivered && t.DeliveredAt != null)
                .Select(t => new { t.ReceivedAt, t.DeliveredAt })
                .ToListAsync();

            double? avgHours = deliveredTickets.Count > 0
                ? deliveredTickets.Average(t => (t.DeliveredAt!.Value - t.ReceivedAt).TotalHours)
                : null;

            result.Add(new TechnicianSummaryItem(tech.FullName, completed, inProgress, avgHours));
        }

        return result.OrderByDescending(t => t.Completed).ToList();
    }

    public async Task<RevenueReportResponse> GetRevenueReportAsync(DateTime? fromDate, DateTime? toDate, RevenueGroupBy groupBy)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30); // mặc định 30 ngày gần nhất nếu không chỉ định
        var to = toDate ?? DateTime.UtcNow;

        // Invoice trong khoảng thời gian (lọc theo CreatedAt, không phải PaidAt — để thấy đủ cả
        // hóa đơn chưa thanh toán phát sinh trong kỳ, phục vụ đúng checklist "Total invoice/Paid/Unpaid")
        var invoicesInRange = await _context.Invoices
            .Where(i => i.CreatedAt >= from && i.CreatedAt <= to)
            .ToListAsync();

        var totalInvoices = invoicesInRange.Count;
        var paidInvoices = invoicesInRange.Where(i => i.PaidAt != null).ToList();
        var unpaidInvoices = totalInvoices - paidInvoices.Count;

        // Revenue = CHỈ tính Invoice đã PaidAt != null (đúng lưu ý Task 7.7: "Invoice chưa thanh toán -> chưa tính revenue")
        var totalRevenue = paidInvoices.Sum(i => i.TotalAmount);

        var grouped = groupBy == RevenueGroupBy.Day
            ? paidInvoices.GroupBy(i => i.PaidAt!.Value.Date.ToString("yyyy-MM-dd"))
            : paidInvoices.GroupBy(i => new DateTime(i.PaidAt!.Value.Year, i.PaidAt!.Value.Month, 1).ToString("yyyy-MM"));

        var items = grouped
            .Select(g => new RevenuePeriodItem(g.Key, g.Sum(i => i.TotalAmount), g.Count()))
            .OrderBy(i => i.Period)
            .ToList();

        return new RevenueReportResponse(items, totalRevenue, totalInvoices, paidInvoices.Count, unpaidInvoices);
    }

    public async Task<List<TechnicianSummaryItem>> GetTechnicianPerformanceAsync(DateTime? fromDate, DateTime? toDate)
    {
        var technicians = await _context.Users
            .Where(u => u.Role.Name == Roles.Technician && u.IsActive)
            .ToListAsync();

        var result = new List<TechnicianSummaryItem>();

        foreach (var tech in technicians)
        {
            var deliveredQuery = _context.RepairTickets
                .Where(t => t.TechnicianId == tech.Id && t.Status.Code == RepairStatusCodes.Delivered && t.DeliveredAt != null);

            if (fromDate is not null) deliveredQuery = deliveredQuery.Where(t => t.DeliveredAt >= fromDate);
            if (toDate is not null) deliveredQuery = deliveredQuery.Where(t => t.DeliveredAt <= toDate);

            var delivered = await deliveredQuery.Select(t => new { t.ReceivedAt, t.DeliveredAt }).ToListAsync();

            var inProgress = await _context.RepairTickets
                .CountAsync(t => t.TechnicianId == tech.Id && InRepairCodes.Contains(t.Status.Code));

            double? avgHours = delivered.Count > 0
                ? delivered.Average(t => (t.DeliveredAt!.Value - t.ReceivedAt).TotalHours)
                : null;

            result.Add(new TechnicianSummaryItem(tech.FullName, delivered.Count, inProgress, avgHours));
        }

        return result.OrderByDescending(t => t.Completed).ToList();
    }
}