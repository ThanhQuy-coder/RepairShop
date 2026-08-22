using RepairShop.Domain.Modules.Quotes;

public interface IQuoteRepository
{
    Task<Quote?> GetByIdAsync(Guid id);
    Task AddAsync(Quote quote);
    Task SaveChangesAsync();
}