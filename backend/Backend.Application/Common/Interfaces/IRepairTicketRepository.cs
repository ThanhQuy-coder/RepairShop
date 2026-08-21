using RepairShop.Domain.Modules.Tickets;

namespace RepairShop.Application.Common.Interfaces;

public interface IRepairTicketRepository
{
    Task<RepairTicket?> GetByIdAsync(Guid id);
    Task<bool> TicketCodeExistsAsync(string ticketCode);
    Task AddAsync(RepairTicket ticket);

    void TrackNewImage(TicketImage image);
    
    Task SaveChangesAsync();
}