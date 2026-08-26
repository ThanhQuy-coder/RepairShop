using RepairShop.Domain.Modules.Billing;
using RepairShop.Domain.Modules.Tickets;
using RepairShop.Domain.Modules.Warranty;

namespace RepairShop.Application.Common.Interfaces;

public interface IRepairTicketRepository
{
    Task<RepairTicket?> GetByIdAsync(Guid id);
    Task<bool> TicketCodeExistsAsync(string ticketCode);
    Task AddAsync(RepairTicket ticket);

    void TrackNewImage(TicketImage image);
    void TrackNewStatusHistory(RepairTicketStatusHistory history);
    void TrackNewTicketPart(TicketPart ticketPart);
    void TrackNewInvoice(Invoice invoice);
    void TrackNewWarranty(Warranty warranty);
    Task<RepairTicket?> GetByTicketCodeForTrackingAsync(string ticketCode);

    Task SaveChangesAsync();
}