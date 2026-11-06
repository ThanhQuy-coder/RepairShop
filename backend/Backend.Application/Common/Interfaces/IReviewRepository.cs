using RepairShop.Domain.Modules.Reviews;

namespace RepairShop.Application.Common.Interfaces;

public record ReviewListItem(Guid Id, int Rating, string? Comment, string CustomerName, bool IsVisible, DateTime CreatedAt);

public record ServiceReviewSummary(
    Guid ServiceId,
    double AverageRating,
    int TotalReviews,
    Dictionary<int, int> RatingDistribution);

public record ServiceReviewItem(
    Guid Id,
    int Rating,
    string? Comment,
    string CustomerDisplayName,
    DateTime CreatedAt);

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(Guid id);
    Task<(List<ReviewListItem> Items, int Total)> SearchAsync(bool? visible, int page, int pageSize);
    Task<(ServiceReviewSummary Summary, List<ServiceReviewItem> Items, int Total)> GetByServiceIdAsync(
        Guid serviceId,
        int page,
        int pageSize);
    void Update(Review review);
    Task SaveChangesAsync();
}
