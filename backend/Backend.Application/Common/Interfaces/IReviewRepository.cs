using RepairShop.Domain.Modules.Reviews;

namespace RepairShop.Application.Common.Interfaces;

public record ReviewListItem(Guid Id, int Rating, string? Comment, string CustomerName, bool IsVisible, DateTime CreatedAt);

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(Guid id);
    Task<(List<ReviewListItem> Items, int Total)> SearchAsync(bool? visible, int page, int pageSize);
    void Update(Review review);
    Task SaveChangesAsync();
}