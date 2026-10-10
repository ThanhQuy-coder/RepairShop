using MediatR;
using RepairShop.Application.Modules.SLA.DTOs;
using RepairShop.Domain.Common.Enums;

namespace RepairShop.Application.Modules.SLA.Commands;

public sealed record CreateSlaPolicyCommand(
    string StatusCode,
    DeviceType? DeviceType,
    int DurationMinutes) : IRequest<SlaPolicyResponse>;
