using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Enums;
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
            where r.DeletedAt == null
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

    public async Task<(ServiceReviewSummary Summary, List<ServiceReviewItem> Items, int Total)> GetByServiceIdAsync(
        Guid serviceId,
        int page,
        int pageSize)
    {
        var baseQuery =
            from r in _context.Reviews
            join t in _context.RepairTickets on r.RepairTicketId equals t.Id
            join st in _context.RepairStatuses on t.StatusId equals st.Id
            join c in _context.Customers on r.CustomerId equals c.Id
            where r.IsVisible
                  && r.DeletedAt == null
                  && t.DeletedAt == null
                  && st.Code == RepairStatusCodes.Delivered
                  && _context.Quotes.Any(q =>
                      q.RepairTicketId == t.Id
                      && q.Status == QuoteStatus.Approved
                      && q.Items.Any(i => i.ServiceId == serviceId))
            select new
            {
                r.Id,
                r.Rating,
                r.Comment,
                CustomerDisplayName = c.FullName,
                r.CreatedAt
            };

        var ratingCounts = await baseQuery
            .GroupBy(x => x.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToListAsync();

        var distribution = new Dictionary<int, int>
        {
            [5] = 0,
            [4] = 0,
            [3] = 0,
            [2] = 0,
            [1] = 0
        };

        var totalReviews = 0;
        var weightedSum = 0;
        foreach (var rc in ratingCounts)
        {
            if (distribution.ContainsKey(rc.Rating))
            {
                distribution[rc.Rating] = rc.Count;
            }
            totalReviews += rc.Count;
            weightedSum += rc.Rating * rc.Count;
        }

        var averageRating = totalReviews > 0
            ? Math.Round((double)weightedSum / totalReviews, 1)
            : 0.0;

        var items = await baseQuery
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ServiceReviewItem(
                x.Id,
                x.Rating,
                x.Comment,
                x.CustomerDisplayName,
                x.CreatedAt))
            .ToListAsync();

        var summary = new ServiceReviewSummary(serviceId, averageRating, totalReviews, distribution);
        return (summary, items, totalReviews);
    }

    public void Update(Review review) => _context.Reviews.Update(review);
    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
