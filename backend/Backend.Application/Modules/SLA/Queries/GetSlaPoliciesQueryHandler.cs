using MediatR;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.SLA.DTOs;

namespace RepairShop.Application.Modules.SLA.Queries;

public sealed class GetSlaPoliciesQueryHandler(ISlaPolicyRepository repository)
    : IRequestHandler<GetSlaPoliciesQuery, IReadOnlyList<SlaPolicyResponse>>
{
    public async Task<IReadOnlyList<SlaPolicyResponse>> Handle(
        GetSlaPoliciesQuery request, CancellationToken cancellationToken)
        => (await repository.GetAllAsync(cancellationToken))
            .Select(policy => new SlaPolicyResponse(policy.Id, policy.StatusCode,
                policy.DeviceType, policy.DurationMinutes, policy.IsActive))
            .ToList();
}
