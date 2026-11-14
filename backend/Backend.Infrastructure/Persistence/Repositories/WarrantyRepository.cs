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

    public async Task<PublicWarrantyLookupView?> GetByWarrantyCodeForPublicAsync(
        string warrantyCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(warrantyCode))
            return null;

        var code = warrantyCode.Trim();
        var row = await (
            from w in _context.Warranties.AsNoTracking()
            join t in _context.RepairTickets on w.RepairTicketId equals t.Id
            join d in _context.Devices on t.DeviceId equals d.Id
            join p in _context.WarrantyPackages on w.WarrantyPackageId equals p.Id into pkgGroup
            from p in pkgGroup.DefaultIfEmpty()
            where w.WarrantyCode == code
            select new
            {
                Warranty = w,
                TicketCode = t.TicketCode,
                DeviceLabel = (d.Brand + " " + d.Model).Trim(),
                PackageName = p != null ? p.Name : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new PublicWarrantyLookupView(row.Warranty, row.TicketCode, row.DeviceLabel, row.PackageName);
    }

    public async Task<List<StaffWarrantyLookupView>> SearchForStaffAsync(
        string? phone, string? warrantyCode, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = phone is null ? null : new string(phone.Where(char.IsDigit).ToArray());
        var query =
            from w in _context.Warranties
            join t in _context.RepairTickets on w.RepairTicketId equals t.Id
            join c in _context.Customers on t.CustomerId equals c.Id
            join d in _context.Devices on t.DeviceId equals d.Id
            select new { Warranty = w, Ticket = t, Customer = c, Device = d };

        if (!string.IsNullOrWhiteSpace(normalizedPhone))
            query = query.Where(x => x.Customer.Phone == normalizedPhone);

        if (!string.IsNullOrWhiteSpace(warrantyCode))
            query = query.Where(x => x.Warranty.WarrantyCode == warrantyCode.Trim());

        var rows = await query
            .AsNoTracking()
            .OrderByDescending(x => x.Warranty.StartDate)
            .Take(50)
            .ToListAsync(cancellationToken);

        return rows.Select(x => new StaffWarrantyLookupView(
            x.Warranty,
            x.Ticket.Id,
            x.Ticket.TicketCode,
            x.Customer.Id,
            x.Customer.FullName,
            x.Customer.Phone,
            x.Device.Id,
            string.Join(" ", new[] { x.Device.Brand, x.Device.Model }
                .Where(value => !string.IsNullOrWhiteSpace(value))))).ToList();
    }
}