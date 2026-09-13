using RepairShop.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace RepairShop.Infrastructure.Persistence.Repositories;

public class WarrantyRepository : IWarrantyRepository
{
    private readonly AppDbContext _context;
    public WarrantyRepository(AppDbContext context) => _context = context;

    public async Task<List<CustomerWarrantyView>> GetByCustomerIdAsync(Guid customerId)
    {
        var query =
            from w in _context.Warranties
            join t in _context.RepairTickets on w.RepairTicketId equals t.Id
            join d in _context.Devices on t.DeviceId equals d.Id
            where t.CustomerId == customerId
            select new
            {
                Warranty = w,
                TicketId = t.Id,
                TicketCode = t.TicketCode,
                DeviceLabel = d.Brand + " " + d.Model,
                StartDate = w.StartDate
            };

        var warranties = await query
            .OrderByDescending(v => v.StartDate)
            .ToListAsync();

        return warranties
            .Select(v => new CustomerWarrantyView(v.Warranty, v.TicketId, v.TicketCode, v.DeviceLabel))
            .ToList();
    }
}