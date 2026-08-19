using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Modules.Tickets;
using Microsoft.EntityFrameworkCore;

namespace RepairShop.Infrastructure.Persistence.Repositories;

public class RepairTicketRepository : IRepairTicketRepository
{
    private readonly AppDbContext _context;

    public RepairTicketRepository(AppDbContext context) => _context = context;

    public Task<RepairTicket?> GetByIdAsync(Guid id) =>
        _context.RepairTickets
            .Include(t => t.Customer)
            .Include(t => t.Device)
            .Include(t => t.Status)
            .Include(t => t.StatusHistories).ThenInclude(h => h.Status)
            .FirstOrDefaultAsync(t => t.Id == id);

    public Task<bool> TicketCodeExistsAsync(string ticketCode) =>
        _context.RepairTickets.AnyAsync(t => t.TicketCode == ticketCode);

    public async Task AddAsync(RepairTicket ticket) => await _context.RepairTickets.AddAsync(ticket);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}