using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Devices.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Devices.Queries;

public class GetDevicesByCustomerQueryHandler : IRequestHandler<GetDevicesByCustomerQuery, List<DeviceResponse>>
{
    private readonly IDeviceRepository _deviceRepository;

    public GetDevicesByCustomerQueryHandler(IDeviceRepository deviceRepository) =>
        _deviceRepository = deviceRepository;

    public async Task<List<DeviceResponse>> Handle(GetDevicesByCustomerQuery request, CancellationToken cancellationToken)
    {
        var devices = await _deviceRepository.GetByCustomerIdAsync(request.CustomerId);

        return devices.Select(d => new DeviceResponse(d.Id, d.CustomerId, d.DeviceType.ToString(),
            d.Brand, d.Model, d.SerialNumber, d.CreatedAt)).ToList();
    }
}