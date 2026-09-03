using Microsoft.EntityFrameworkCore;
using RepairShop.Domain.Modules.Quotes;
using RepairShop.Infrastructure.Persistence;

public class QuoteRepository : IQuoteRepository
{
    private readonly AppDbContext _context;
    public QuoteRepository(AppDbContext context) => _context = context;

    public Task<Quote?> GetByIdAsync(Guid id) =>
        _context.Quotes
            .Include(q => q.RepairTicket)
                .ThenInclude(t => t.Status)
            .Include(q => q.Items)
            .AsSplitQuery()
            .FirstOrDefaultAsync(q => q.Id == id);

    // AddAsync gọi trên Quote MỚI, chưa từng tracked -> EF Core tự cascade-Added cả QuoteItems
    // bên trong navigation (khác trường hợp TicketImage/StatusHistory ở Task 4.5/4.6, vì ở đó
    // entity CON được thêm vào entity CHA đã tracked Unchanged — còn ở đây Quote chính là root mới).
    public async Task AddAsync(Quote quote) => await _context.Quotes.AddAsync(quote);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}