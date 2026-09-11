using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Content.DTOs;
using RepairShop.Domain.Modules.Content;
using MediatR;

namespace RepairShop.Application.Modules.Content.Commands;

public class CreateServiceCommandHandler : IRequestHandler<CreateServiceCommand, ServiceResponse>
{
    private readonly IServiceRepository _serviceRepository;
    public CreateServiceCommandHandler(IServiceRepository serviceRepository) => _serviceRepository = serviceRepository;

    public async Task<ServiceResponse> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
    {
        var service = new Service(request.Name, request.Description, request.BasePrice, request.DeviceType);
        await _serviceRepository.AddAsync(service);
        await _serviceRepository.SaveChangesAsync();

        return new ServiceResponse(service.Id, service.Name, service.Description, service.BasePrice, service.DeviceType, service.IsActive);
    }
}