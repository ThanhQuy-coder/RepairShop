using RepairShop.Domain.Modules.Billing;
using RepairShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _context;
    public InvoiceRepository(AppDbContext context) => _context = context;

    public Task<Invoice?> GetByIdAsync(Guid id) => _context.Invoices.FirstOrDefaultAsync(i => i.Id == id);
    public Task SaveChangesAsync() => _context.SaveChangesAsync();

}