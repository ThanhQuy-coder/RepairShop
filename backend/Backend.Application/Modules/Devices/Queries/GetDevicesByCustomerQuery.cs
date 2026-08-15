using RepairShop.Application.Modules.Devices.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Devices.Queries;

public record GetDevicesByCustomerQuery(Guid CustomerId) : IRequest<List<DeviceResponse>>;