using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Devices.DTOs;
using RepairShop.Domain.Common.Enums;
using MediatR;

namespace RepairShop.Application.Modules.Devices.Commands;

public class CreateDeviceCommandHandler : IRequestHandler<CreateDeviceCommand, DeviceResponse>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly ICustomerRepository _customerRepository;

    public CreateDeviceCommandHandler(IDeviceRepository deviceRepository, ICustomerRepository customerRepository)
    {
        _deviceRepository = deviceRepository;
        _customerRepository = customerRepository;
    }

    public async Task<DeviceResponse> Handle(CreateDeviceCommand request, CancellationToken cancellationToken)
    {
        // Đảm bảo Device luôn gắn với 1 Customer có thật (composition đã chốt ở Class Diagram Tuần 2)
        _ = await _customerRepository.GetByIdAsync(request.CustomerId)
            ?? throw new KeyNotFoundException($"Không tìm thấy khách hàng với Id '{request.CustomerId}'.");

        if (!Enum.TryParse<DeviceType>(request.DeviceType, true, out var deviceType))
            throw new ArgumentException($"DeviceType '{request.DeviceType}' không hợp lệ.");

        var device = new RepairShop.Domain.Modules.Devices.Device(
            request.CustomerId, deviceType, request.Brand, request.Model, request.SerialNumber);

        await _deviceRepository.AddAsync(device);
        await _deviceRepository.SaveChangesAsync();

        return new DeviceResponse(device.Id, device.CustomerId, device.DeviceType.ToString(),
            device.Brand, device.Model, device.SerialNumber, device.CreatedAt);
    }
}