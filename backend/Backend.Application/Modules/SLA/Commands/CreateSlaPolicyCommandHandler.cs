using MediatR;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.SLA.DTOs;
using RepairShop.Domain.Modules.SLA;

namespace RepairShop.Application.Modules.SLA.Commands;

public sealed class CreateSlaPolicyCommandHandler(ISlaPolicyRepository repository)
    : IRequestHandler<CreateSlaPolicyCommand, SlaPolicyResponse>
{
    public async Task<SlaPolicyResponse> Handle(
        CreateSlaPolicyCommand request, CancellationToken cancellationToken)
    {
        if (await repository.ExistsAsync(request.StatusCode, request.DeviceType, cancellationToken))
            throw new InvalidOperationException("Đã tồn tại chính sách SLA cho tổ hợp này.");

        var policy = new SLAPolicy(request.StatusCode, request.DeviceType, request.DurationMinutes);
        await repository.AddAsync(policy, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return new SlaPolicyResponse(policy.Id, policy.StatusCode, policy.DeviceType,
            policy.DurationMinutes, policy.IsActive);
    }
}
