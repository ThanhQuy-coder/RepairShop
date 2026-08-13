using Backend.Application.Common.Interfaces;
using Backend.Application.Modules.Devices.DTOs;
using MediatR;

namespace Backend.Application.Modules.Devices.Queries;

public class GetDeviceByIdQueryHandler : IRequestHandler<GetDeviceByIdQuery, DeviceResponse>
{
    private readonly IDeviceRepository _deviceRepository;

    public GetDeviceByIdQueryHandler(IDeviceRepository deviceRepository) => _deviceRepository = deviceRepository;

    public async Task<DeviceResponse> Handle(GetDeviceByIdQuery request, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException($"Không tìm thấy thiết bị với Id '{request.Id}'.");

        return new DeviceResponse(device.Id, device.CustomerId, device.DeviceType.ToString(),
            device.Brand, device.Model, device.SerialNumber, device.CreatedAt);
    }
}