using Backend.Application.Modules.Devices.DTOs;
using MediatR;

namespace Backend.Application.Modules.Devices.Queries;

public record GetDevicesByCustomerQuery(Guid CustomerId) : IRequest<List<DeviceResponse>>;