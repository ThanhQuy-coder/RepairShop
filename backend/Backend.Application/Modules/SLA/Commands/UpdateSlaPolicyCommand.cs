using MediatR;

namespace RepairShop.Application.Modules.SLA.Commands;

public sealed record UpdateSlaPolicyCommand(
    Guid Id,
    int DurationMinutes,
    bool IsActive) : IRequest;
