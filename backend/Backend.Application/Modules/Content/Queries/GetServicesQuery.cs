using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Content.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Content.Queries;

// Public: chỉ isActive=true. Admin: xem tất cả (isActive=null)
public record GetServicesQuery(
    bool? IsActive,
    int Page = 1,
    int PageSize = 50,
    string? DeviceType = null,
    string? Search = null) : IRequest<ServiceListResponse>;

public record GetServiceByIdQuery(Guid Id) : IRequest<ServiceResponse>;

public class GetServicesQueryHandler :
    IRequestHandler<GetServicesQuery, ServiceListResponse>,
    IRequestHandler<GetServiceByIdQuery, ServiceResponse>
{
    private readonly IServiceRepository _serviceRepository;
    public GetServicesQueryHandler(IServiceRepository serviceRepository) => _serviceRepository = serviceRepository;

    public async Task<ServiceListResponse> Handle(GetServicesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var (items, total) = await _serviceRepository.SearchAsync(
            request.IsActive,
            page,
            pageSize,
            request.DeviceType,
            request.Search);

        var ratingStats = await _serviceRepository.GetServiceRatingStatsAsync(items.Select(s => s.Id));

        var responses = items.Select(s =>
        {
            var (avgRating, reviewCount) = ratingStats.GetValueOrDefault(s.Id, (0.0, 0));
            return new ServiceResponse(
                s.Id,
                s.Name,
                s.Description,
                s.BasePrice,
                s.DeviceType,
                s.IsActive,
                s.ImageUrl,
                avgRating,
                reviewCount);
        }).ToList();

        return new ServiceListResponse(responses, total);
    }

    public async Task<ServiceResponse> Handle(GetServiceByIdQuery request, CancellationToken cancellationToken)
    {
        var service = await _serviceRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Dịch vụ", request.Id);

        var ratingStats = await _serviceRepository.GetServiceRatingStatsAsync([service.Id]);
        var (avgRating, reviewCount) = ratingStats.GetValueOrDefault(service.Id, (0.0, 0));

        return new ServiceResponse(
            service.Id,
            service.Name,
            service.Description,
            service.BasePrice,
            service.DeviceType,
            service.IsActive,
            service.ImageUrl,
            avgRating,
            reviewCount);
    }
}
