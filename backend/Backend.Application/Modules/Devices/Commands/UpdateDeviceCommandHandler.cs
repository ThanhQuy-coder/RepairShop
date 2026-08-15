using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Devices.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Devices.Commands;

public class UpdateDeviceCommandHandler : IRequestHandler<UpdateDeviceCommand, DeviceResponse>
{
    private readonly IDeviceRepository _deviceRepository;

    public UpdateDeviceCommandHandler(IDeviceRepository deviceRepository) => _deviceRepository = deviceRepository;

    public async Task<DeviceResponse> Handle(UpdateDeviceCommand request, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException($"Không tìm thấy thiết bị với Id '{request.Id}'.");

        device.UpdateInfo(request.Brand, request.Model, request.SerialNumber);

        _deviceRepository.Update(device);
        await _deviceRepository.SaveChangesAsync();

        return new DeviceResponse(device.Id, device.CustomerId, device.DeviceType.ToString(),
            device.Brand, device.Model, device.SerialNumber, device.CreatedAt);
    }
}