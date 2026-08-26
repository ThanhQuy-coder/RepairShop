using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Modules.Tickets;
using Microsoft.EntityFrameworkCore;
using RepairShop.Domain.Modules.Billing;
using RepairShop.Domain.Modules.Warranty;

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
            .Include(t => t.StatusHistories).ThenInclude(h => h.Status)
            .Include(t => t.StatusHistories).ThenInclude(h => h.ChangedByUser)
            .Include(t => t.Images)
            .Include(t => t.Invoice)
            .Include(t => t.Warranty)
            .AsSplitQuery()
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
}