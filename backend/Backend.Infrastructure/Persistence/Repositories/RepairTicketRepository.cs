using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Modules.Tickets;
using Microsoft.EntityFrameworkCore;
using RepairShop.Domain.Modules.Billing;
using RepairShop.Domain.Modules.Warranty;
using RepairShop.Domain.Common;

namespace RepairShop.Infrastructure.Persistence.Repositories;

public class RepairTicketRepository : IRepairTicketRepository
{
    private readonly AppDbContext _context;

    public RepairTicketRepository(AppDbContext context) => _context = context;

    public void TrackNewImage(TicketImage image) =>
        _context.Entry(image).State = EntityState.Added;

    public void TrackNewStatusHistory(RepairTicketStatusHistory history) =>
        _context.Entry(history).State = EntityState.Added;

    public Task<RepairTicket?> GetByIdAsync(Guid id) =>
        _context.RepairTickets
            .Include(t => t.Customer)
            .Include(t => t.Device)
            .Include(t => t.Status)
            .Include(t => t.Quotes).ThenInclude(q => q.Items)
            .Include(t => t.Technician)
            .Include(t => t.TicketParts).ThenInclude(tp => tp.Part)
            .Include(t => t.StatusHistories).ThenInclude(h => h.Status)
            .Include(t => t.StatusHistories).ThenInclude(h => h.ChangedByUser)
            .Include(t => t.Images)
            .Include(t => t.Invoice)
            .Include(t => t.Warranty)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == id);

    public Task<RepairTicket?> GetByIdForQueryAsync(Guid id) =>
        _context.RepairTickets
            .Include(t => t.Customer)
            .Include(t => t.Device)
            .Include(t => t.Status)
            .Include(t => t.Technician)
            .Include(t => t.TicketParts).ThenInclude(tp => tp.Part)
            .Include(t => t.Images)
            .Include(t => t.Invoice)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

    public Task<bool> TicketCodeExistsAsync(string ticketCode) =>
        _context.RepairTickets.AnyAsync(t => t.TicketCode == ticketCode);

    public async Task AddAsync(RepairTicket ticket) => await _context.RepairTickets.AddAsync(ticket);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();

    public void TrackNewTicketPart(TicketPart ticketPart) =>
        _context.Entry(ticketPart).State = EntityState.Added;

    public void TrackNewInvoice(Invoice invoice) =>
        _context.Entry(invoice).State = EntityState.Added;

    public void TrackNewWarranty(Warranty warranty)
        => _context.Entry(warranty).State = EntityState.Added;

    public Task<RepairTicket?> GetByTicketCodeForTrackingAsync(string ticketCode) =>
        _context.RepairTickets
            .Include(t => t.Device)
            .Include(t => t.Status)
            .Include(t => t.StatusHistories).ThenInclude(h => h.Status)
            .AsSplitQuery()
            // Chủ động KHÔNG Include Customer, Receptionist, Technician, Images, Notes...
            // -> không phải "che dữ liệu sau khi lấy", mà TỪ ĐẦU không kéo dữ liệu nhạy cảm vào bộ nhớ.
            .AsNoTracking() // read-only, public, không cần EF theo dõi thay đổi -> tối ưu + rõ ý đồ "chỉ đọc"
            .FirstOrDefaultAsync(t => t.TicketCode == ticketCode);

    public async Task<(List<RepairTicket> Items, int Total)> SearchAsync(
        string? statusCode, Guid? technicianId, Guid? customerId,
        Guid? currentUserId, string? currentUserRole,
        int page, int pageSize)
    {
        var query = _context.RepairTickets
            .Include(t => t.Customer)
            .Include(t => t.Device)
            .Include(t => t.Technician)
            .Include(t => t.Status)
            .AsQueryable();

        // Ownership ở tầng query: Technician CHỈ thấy ticket của mình trong danh sách — khớp đúng nguyên tắc
        // TicketAccessGuard (Task 4.6/4.16, Tuần 4), áp dụng ngay từ bước lọc, không phải lọc sau khi trả về.
        if (currentUserRole == Roles.Technician && currentUserId is not null)
            query = query.Where(t => t.TechnicianId == currentUserId);

        if (currentUserRole == Roles.Customer && currentUserId is not null)
            query = query.Where(t => t.Customer.UserId == currentUserId);

        if (!string.IsNullOrWhiteSpace(statusCode))
            query = query.Where(t => t.Status.Code == statusCode);

        if (technicianId is not null)
            query = query.Where(t => t.TechnicianId == technicianId);

        if (customerId is not null)
            query = query.Where(t => t.CustomerId == customerId);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.ReceivedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync();

        return (items, total);
    }
}