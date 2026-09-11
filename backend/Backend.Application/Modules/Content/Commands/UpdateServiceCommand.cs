using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Content.DTOs;

public record UpdateServiceCommand(Guid Id, string Name, string? Description, decimal? BasePrice, string? DeviceType)
    : IRequest<ServiceResponse>;

public class UpdateServiceCommandHandler : IRequestHandler<UpdateServiceCommand, ServiceResponse>
{
    private readonly IServiceRepository _serviceRepository;
    public UpdateServiceCommandHandler(IServiceRepository serviceRepository) => _serviceRepository = serviceRepository;

    public async Task<ServiceResponse> Handle(UpdateServiceCommand request, CancellationToken cancellationToken)
    {
        var service = await _serviceRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Dịch vụ", request.Id);

        service.UpdateInfo(request.Name, request.Description, request.BasePrice, request.DeviceType);
        _serviceRepository.Update(service);
        await _serviceRepository.SaveChangesAsync();

        return new ServiceResponse(service.Id, service.Name, service.Description, service.BasePrice, service.DeviceType, service.IsActive);
    }
}