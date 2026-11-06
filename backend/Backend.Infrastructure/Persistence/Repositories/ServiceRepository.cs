using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Modules.Content;
using Microsoft.EntityFrameworkCore;

namespace RepairShop.Infrastructure.Persistence.Repositories;

public class ServiceRepository : IServiceRepository
{
    private readonly AppDbContext _context;
    public ServiceRepository(AppDbContext context) => _context = context;

    public Task<Service?> GetByIdAsync(Guid id) =>
        _context.Services.FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null);

    public Task<List<Service>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.Distinct().ToList();
        return _context.Services.Where(s => idList.Contains(s.Id) && s.DeletedAt == null).ToListAsync();
    }

    public async Task<(List<Service> Items, int Total)> SearchAsync(
        bool? isActive,
        int page,
        int pageSize,
        string? deviceType = null,
        string? search = null)
    {
        var query = _context.Services.Where(s => s.DeletedAt == null).AsQueryable();
        if (isActive is not null) query = query.Where(s => s.IsActive == isActive);

        if (!string.IsNullOrWhiteSpace(deviceType))
        {
            var dt = deviceType.Trim();
            query = query.Where(s => s.DeviceType == dt);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(keyword) ||
                (s.Description != null && s.Description.ToLower().Contains(keyword)) ||
                (s.DeviceType != null && s.DeviceType.ToLower().Contains(keyword)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(s => s.DeviceType)
            .ThenBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    public async Task<Dictionary<Guid, (double AverageRating, int ReviewCount)>> GetServiceRatingStatsAsync(IEnumerable<Guid> serviceIds)
    {
        var ids = serviceIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, (double AverageRating, int ReviewCount)>();
        }

        var distinctServiceReviews =
            (from r in _context.Reviews
             join t in _context.RepairTickets on r.RepairTicketId equals t.Id
             join st in _context.RepairStatuses on t.StatusId equals st.Id
             join q in _context.Quotes on t.Id equals q.RepairTicketId
             join qi in _context.QuoteItems on q.Id equals qi.QuoteId
             where r.IsVisible
                   && r.DeletedAt == null
                   && t.DeletedAt == null
                   && st.Code == RepairStatusCodes.Delivered
                   && q.Status == QuoteStatus.Approved
                   && qi.ServiceId != null
                   && ids.Contains(qi.ServiceId.Value)
             select new
             {
                 ServiceId = qi.ServiceId!.Value,
                 ReviewId = r.Id,
                 r.Rating
             }).Distinct();

        var grouped = await distinctServiceReviews
            .GroupBy(x => x.ServiceId)
            .Select(g => new
            {
                ServiceId = g.Key,
                AverageRating = g.Average(x => (double)x.Rating),
                ReviewCount = g.Count()
            })
            .ToListAsync();

        return grouped.ToDictionary(
            x => x.ServiceId,
            x => (Math.Round(x.AverageRating, 1), x.ReviewCount));
    }

    public async Task AddAsync(Service service) => await _context.Services.AddAsync(service);
    public void Update(Service service) => _context.Services.Update(service);
    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
