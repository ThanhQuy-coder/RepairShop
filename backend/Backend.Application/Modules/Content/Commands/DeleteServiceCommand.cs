using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;

public record ToggleServicePublishCommand(Guid Id, bool Publish) : IRequest<Unit>;

public class ToggleServicePublishCommandHandler : IRequestHandler<ToggleServicePublishCommand, Unit>
{
    private readonly IServiceRepository _serviceRepository;
    public ToggleServicePublishCommandHandler(IServiceRepository serviceRepository) => _serviceRepository = serviceRepository;

    public async Task<Unit> Handle(ToggleServicePublishCommand request, CancellationToken cancellationToken)
    {
        var service = await _serviceRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Dịch vụ", request.Id);

        if (request.Publish) service.Publish(); else service.Unpublish();

        _serviceRepository.Update(service);
        await _serviceRepository.SaveChangesAsync();
        return Unit.Value;
    }
}