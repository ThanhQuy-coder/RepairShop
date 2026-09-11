using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Modules.Content;
using Microsoft.EntityFrameworkCore;

namespace RepairShop.Infrastructure.Persistence.Repositories;

public class ArticleRepository : IArticleRepository
{
    private readonly AppDbContext _context;
    public ArticleRepository(AppDbContext context) => _context = context;

    public Task<Article?> GetByIdAsync(Guid id) => _context.Articles.FirstOrDefaultAsync(a => a.Id == id);

    public async Task<(List<Article> Items, int Total)> SearchAsync(bool? published, int page, int pageSize)
    {
        var query = _context.Articles.AsQueryable();
        if (published is not null) query = query.Where(a => a.IsPublished == published);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(a => a.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task AddAsync(Article article) => await _context.Articles.AddAsync(article);
    public void Update(Article article) => _context.Articles.Update(article);
    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}