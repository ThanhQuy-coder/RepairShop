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
        var from = ToUtc(fromDate) ?? DateTime.UtcNow.AddDays(-30); // mặc định 30 ngày gần nhất nếu không chỉ định
        var to = ToUtc(toDate) ?? DateTime.UtcNow;

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


    public async Task<List<TechnicianSummaryItem>> GetTechnicianPerformanceAsync(
        DateTime? fromDate,
        DateTime? toDate)
    {
        var from = ToUtc(fromDate);
        var to = ToUtc(toDate);

        // 1. Get active technicians
        var technicians = await _context.Users
            .AsNoTracking()
            .Where(u => u.Role.Name == Roles.Technician && u.IsActive)
            .Select(u => new
            {
                u.Id,
                u.FullName
            })
            .ToListAsync();

        if (technicians.Count == 0)
            return [];

        var technicianIds = technicians.Select(t => t.Id).ToList();

        // 2. Aggregate delivered tickets
        var deliveredQuery = _context.RepairTickets
            .AsNoTracking()
            .Where(t =>
                t.TechnicianId.HasValue &&
                technicianIds.Contains(t.TechnicianId.Value) &&
                t.Status.Code == RepairStatusCodes.Delivered &&
                t.DeliveredAt != null);

        if (from.HasValue)
            deliveredQuery = deliveredQuery
                .Where(t => t.DeliveredAt >= from.Value);

        if (to.HasValue)
            deliveredQuery = deliveredQuery
                .Where(t => t.DeliveredAt <= to.Value);

        var deliveredStats = await deliveredQuery
            .GroupBy(t => t.TechnicianId)
            .Select(g => new
            {
                TechnicianId = g.Key!.Value,
                Completed = g.Count(),
                AverageHours = g.Average(t =>
                    (t.DeliveredAt!.Value - t.ReceivedAt).TotalHours)
            })
            .ToDictionaryAsync(x => x.TechnicianId);

        // 3. Aggregate in-progress tickets
        var inProgressStats = await _context.RepairTickets
            .AsNoTracking()
            .Where(t =>
                t.TechnicianId.HasValue &&
                technicianIds.Contains(t.TechnicianId.Value) &&
                InRepairCodes.Contains(t.Status.Code))
            .GroupBy(t => t.TechnicianId)
            .Select(g => new
            {
                TechnicianId = g.Key!.Value,
                InProgress = g.Count()
            })
            .ToDictionaryAsync(x => x.TechnicianId);

        // 4. Combine results
        return technicians
            .Select(tech =>
            {
                deliveredStats.TryGetValue(tech.Id, out var delivered);
                inProgressStats.TryGetValue(tech.Id, out var inProgress);

                return new TechnicianSummaryItem(
                    tech.FullName,
                    delivered?.Completed ?? 0,
                    inProgress?.InProgress ?? 0,
                    delivered?.AverageHours
                );
            })
            .OrderByDescending(x => x.Completed)
            .ToList();
    }


    public async Task<ProfitReportResponse> GetProfitReportAsync(
        DateTime? fromDate, DateTime? toDate)
    {
        var from = ToUtc(fromDate) ?? DateTime.UtcNow.AddDays(-30);
        var toExclusive = ToUtc(toDate)?.Date.AddDays(1) ?? DateTime.UtcNow;

        var invoices = await _context.Invoices
            .Where(invoice => invoice.PaidAt != null &&
                invoice.PaidAt >= from && invoice.PaidAt < toExclusive)
            .ToListAsync();
        var ticketIds = invoices.Select(invoice => invoice.RepairTicketId).ToList();
        var tickets = await _context.RepairTickets
            .Where(ticket => ticketIds.Contains(ticket.Id))
            .Include(ticket => ticket.TicketParts)
            .ToDictionaryAsync(ticket => ticket.Id);

        var quoteIds = invoices
            .Where(invoice => invoice.QuoteId.HasValue)
            .Select(invoice => invoice.QuoteId!.Value)
            .Distinct()
            .ToList();
        var quotes = await _context.Quotes
            .Where(quote => quoteIds.Contains(quote.Id))
            .Include(quote => quote.Items)
                .ThenInclude(item => item.Service)
            .ToDictionaryAsync(quote => quote.Id);

        var items = invoices.Select(invoice =>
        {
            var revenue = invoice.TotalAmount;
            var cost = tickets[invoice.RepairTicketId].TicketParts.Sum(part => part.CostSubtotal);
            var profit = revenue - cost;
            var margin = revenue == 0 ? 0 : profit / revenue * 100;
            return new ProfitReportItem(
                tickets[invoice.RepairTicketId].TicketCode, revenue, cost, profit, margin);
        }).ToList();

        var serviceRows = new List<(string Name, Guid? ServiceId, Guid TicketId, decimal Revenue)>();
        foreach (var invoice in invoices)
        {
            var ticket = tickets[invoice.RepairTicketId];
            var cost = ticket.TicketParts.Sum(part => part.CostSubtotal);
            var serviceRevenue = quotes.TryGetValue(invoice.QuoteId ?? Guid.Empty, out var quote)
                ? quote.Items
                    .Where(item => item.ServiceId.HasValue)
                    .GroupBy(item => new { item.ServiceId, Name = item.Service?.Name ?? item.Description })
                    .Select(group => (Name: group.Key.Name, ServiceId: group.Key.ServiceId, Revenue: group.Sum(item => item.Subtotal)))
                    .ToList()
                : [];

            var totalServiceRevenue = serviceRevenue.Sum(row => row.Revenue);
            foreach (var row in serviceRevenue)
                serviceRows.Add((row.Name, row.ServiceId, invoice.RepairTicketId, row.Revenue));

            var otherRevenue = invoice.TotalAmount - totalServiceRevenue;
            if (otherRevenue > 0)
                serviceRows.Add(("Khác / linh kiện", null, invoice.RepairTicketId, otherRevenue));

            // Legacy invoices may not have a linked quote. Keep them visible instead of
            // silently dropping paid revenue from the service breakdown.
            if (serviceRevenue.Count == 0 && otherRevenue <= 0)
                serviceRows.Add(("Chưa phân loại", null, invoice.RepairTicketId, invoice.TotalAmount));
        }

        var byService = serviceRows
            .GroupBy(row => row.Name)
            .Select(group =>
            {
                var revenue = group.Sum(row => row.Revenue);
                var groupCost = group
                    .GroupBy(row => row.TicketId)
                    .Sum(ticketRows =>
                    {
                        var ticketCost = tickets[ticketRows.Key].TicketParts.Sum(part => part.CostSubtotal);
                        var ticketRevenue = ticketRows.Sum(row => row.Revenue);
                        var invoiceRevenue = invoices
                            .Where(invoice => invoice.RepairTicketId == ticketRows.Key)
                            .Sum(invoice => invoice.TotalAmount);
                        return invoiceRevenue == 0 ? 0 : ticketCost * ticketRevenue / invoiceRevenue;
                    });
                var grossProfit = revenue - groupCost;
                return new ProfitByServiceItem(
                    group.Key,
                    group.Select(row => row.TicketId).Distinct().Count(),
                    revenue,
                    groupCost,
                    grossProfit,
                    revenue == 0 ? 0 : grossProfit / revenue * 100);
            })
            .OrderByDescending(item => item.GrossProfit)
            .ToList();

        var totalRevenue = items.Sum(item => item.Revenue);
        var totalCost = items.Sum(item => item.Cost);
        var grossProfit = items.Sum(item => item.GrossProfit);
        var marginPercent = totalRevenue == 0 ? 0 : grossProfit / totalRevenue * 100;
        return new ProfitReportResponse(items, totalRevenue, totalCost, grossProfit, marginPercent, byService);
    }

    private static DateTime? ToUtc(DateTime? value)
    {
        if (value is null) return null;

        return value.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : value.Value.ToUniversalTime();
    }

    private static readonly Dictionary<string, string> StatusLabels = new()
    {
        [RepairStatusCodes.CheckedIn] = "Đã tiếp nhận",
        [RepairStatusCodes.Assigned] = "Đã phân công",
        [RepairStatusCodes.Diagnosing] = "Đang chẩn đoán",
        [RepairStatusCodes.WaitingApproval] = "Chờ xác nhận",
        [RepairStatusCodes.OnHold] = "Tạm hoãn",
        [RepairStatusCodes.WaitingParts] = "Chờ linh kiện",
        [RepairStatusCodes.InRepair] = "Đang sửa chữa",
        [RepairStatusCodes.QaTesting] = "Đang kiểm thử",
        [RepairStatusCodes.ReadyForPickup] = "Sẵn sàng bàn giao",
        [RepairStatusCodes.Delivered] = "Đã hoàn thành",
        [RepairStatusCodes.ClosedRejected] = "Đã đóng",
    };

    public async Task<List<StatusBreakdownItem>> GetStatusBreakdownAsync()
    {
        var grouped = await _context.RepairTickets
            .GroupBy(t => t.Status.Code)
            .Select(g => new { StatusCode = g.Key, Count = g.Count() })
            .ToListAsync();

        return grouped
            .Select(g => new StatusBreakdownItem(g.StatusCode, StatusLabels.GetValueOrDefault(g.StatusCode, g.StatusCode), g.Count))
            .OrderByDescending(s => s.Count)
            .ToList();
    }

    public Task<int> GetTotalCustomersAsync() => _context.Customers.CountAsync();
}