using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Content.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Content.Queries;

// Public: chỉ isActive=true. Admin: xem tất cả (isActive=null)
public record GetServicesQuery(bool? IsActive, int Page = 1, int PageSize = 50) : IRequest<ServiceListResponse>;

public class GetServicesQueryHandler : IRequestHandler<GetServicesQuery, ServiceListResponse>
{
    private readonly IServiceRepository _serviceRepository;
    public GetServicesQueryHandler(IServiceRepository serviceRepository) => _serviceRepository = serviceRepository;

    public async Task<ServiceListResponse> Handle(GetServicesQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _serviceRepository.SearchAsync(request.IsActive, request.Page, request.PageSize);
        var responses = items.Select(s => new ServiceResponse(s.Id, s.Name, s.Description, s.BasePrice, s.DeviceType, s.IsActive)).ToList();
        return new ServiceListResponse(responses, total);
    }
}