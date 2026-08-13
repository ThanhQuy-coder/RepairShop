using Backend.Application.Modules.Devices.DTOs;
using MediatR;

namespace Backend.Application.Modules.Devices.Commands;

public record CreateDeviceCommand(Guid CustomerId, string DeviceType, string Brand, string Model,
    string? SerialNumber) : IRequest<DeviceResponse>;