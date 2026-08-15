using RepairShop.Application.Modules.Devices.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Devices.Commands;

public record UpdateDeviceCommand(Guid Id, string Brand, string Model, string? SerialNumber)
    : IRequest<DeviceResponse>;