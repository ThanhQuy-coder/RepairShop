using Backend.Application.Modules.Devices.DTOs;
using MediatR;

namespace Backend.Application.Modules.Devices.Commands;

public record UpdateDeviceCommand(Guid Id, string Brand, string Model, string? SerialNumber)
    : IRequest<DeviceResponse>;