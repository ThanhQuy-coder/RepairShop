using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using MediatR;

namespace RepairShop.Application.Modules.Reviews.Queries;

public record ReviewListResponse(List<ReviewListItem> Items, int Total);

public record ServiceReviewListResponse(
    ServiceReviewSummary Summary,
    List<ServiceReviewItem> Items,
    int Total,
    int Page,
    int PageSize);

// visible=null nghĩa là Admin xem toàn bộ; Controller quyết định truyền gì tùy route
public record GetReviewsQuery(bool? Visible, int Page = 1, int PageSize = 20) : IRequest<ReviewListResponse>;

public record GetServiceReviewsQuery(Guid ServiceId, int Page = 1, int PageSize = 10) : IRequest<ServiceReviewListResponse>;

public class GetReviewsQueryHandler :
    IRequestHandler<GetReviewsQuery, ReviewListResponse>,
    IRequestHandler<GetServiceReviewsQuery, ServiceReviewListResponse>
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IServiceRepository? _serviceRepository;

    public GetReviewsQueryHandler(
        IReviewRepository reviewRepository,
        IServiceRepository? serviceRepository = null)
    {
        _reviewRepository = reviewRepository;
        _serviceRepository = serviceRepository;
    }

    public async Task<ReviewListResponse> Handle(GetReviewsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var (items, total) = await _reviewRepository.SearchAsync(request.Visible, page, pageSize);
        return new ReviewListResponse(items, total);
    }

    public async Task<ServiceReviewListResponse> Handle(GetServiceReviewsQuery request, CancellationToken cancellationToken)
    {
        if (_serviceRepository is not null)
        {
            _ = await _serviceRepository.GetByIdAsync(request.ServiceId)
                ?? throw new NotFoundException("Dịch vụ", request.ServiceId);
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var (summary, items, total) = await _reviewRepository.GetByServiceIdAsync(request.ServiceId, page, pageSize);
        return new ServiceReviewListResponse(summary, items, total, page, pageSize);
    }
}
