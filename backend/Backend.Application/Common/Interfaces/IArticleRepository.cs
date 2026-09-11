using RepairShop.Domain.Modules.Content;

namespace RepairShop.Application.Common.Interfaces;

public interface IArticleRepository
{
    Task<Article?> GetByIdAsync(Guid id);
    Task<(List<Article> Items, int Total)> SearchAsync(bool? published, int page, int pageSize);
    Task AddAsync(Article article);
    void Update(Article article);
    Task SaveChangesAsync();
}