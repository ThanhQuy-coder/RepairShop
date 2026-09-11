using RepairShop.Application.Common.Interfaces;
using MediatR;

namespace RepairShop.Application.Modules.Reviews.Queries;

public record ReviewListResponse(List<ReviewListItem> Items, int Total);

// visible=null nghĩa là Admin xem toàn bộ; Controller quyết định truyền gì tùy route
public record GetReviewsQuery(bool? Visible, int Page = 1, int PageSize = 20) : IRequest<ReviewListResponse>;

public class GetReviewsQueryHandler : IRequestHandler<GetReviewsQuery, ReviewListResponse>
{
    private readonly IReviewRepository _reviewRepository;
    public GetReviewsQueryHandler(IReviewRepository reviewRepository) => _reviewRepository = reviewRepository;

    public async Task<ReviewListResponse> Handle(GetReviewsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _reviewRepository.SearchAsync(request.Visible, request.Page, request.PageSize);
        return new ReviewListResponse(items, total);
    }
}