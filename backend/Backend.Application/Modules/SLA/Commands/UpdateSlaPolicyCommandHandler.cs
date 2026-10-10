using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;

namespace RepairShop.Application.Modules.SLA.Commands;

public sealed class UpdateSlaPolicyCommandHandler(ISlaPolicyRepository repository)
    : IRequestHandler<UpdateSlaPolicyCommand>
{
    public async Task Handle(UpdateSlaPolicyCommand request, CancellationToken cancellationToken)
    {
        var policy = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("SLA policy", request.Id);
        policy.Update(request.DurationMinutes, request.IsActive);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
