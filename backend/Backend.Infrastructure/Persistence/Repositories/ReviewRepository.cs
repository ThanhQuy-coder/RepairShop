using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Modules.Reviews;
using Microsoft.EntityFrameworkCore;

namespace RepairShop.Infrastructure.Persistence.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly AppDbContext _context;
    public ReviewRepository(AppDbContext context) => _context = context;

    public Task<Review?> GetByIdAsync(Guid id) => _context.Reviews.FirstOrDefaultAsync(r => r.Id == id);

    public async Task<(List<ReviewListItem> Items, int Total)> SearchAsync(bool? visible, int page, int pageSize)
    {
        var query =
            from r in _context.Reviews
            join c in _context.Customers on r.CustomerId equals c.Id
            select new { r, c.FullName };

        if (visible is not null) query = query.Where(x => x.r.IsVisible == visible);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new ReviewListItem(x.r.Id, x.r.Rating, x.r.Comment, x.FullName, x.r.IsVisible, x.r.CreatedAt))
            .ToListAsync();

        return (items, total);
    }

    public void Update(Review review) => _context.Reviews.Update(review);
    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}