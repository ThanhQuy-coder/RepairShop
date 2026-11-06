using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Devices.DTOs;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Common.Exceptions;
using MediatR;
using RepairShop.Domain.Modules.Devices;

namespace RepairShop.Application.Modules.Devices.Commands;

public class CreateDeviceCommandHandler : IRequestHandler<CreateDeviceCommand, DeviceResponse>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IDeviceModelRepository? _deviceModelRepository;

    public CreateDeviceCommandHandler(
        IDeviceRepository deviceRepository,
        ICustomerRepository customerRepository,
        IDeviceModelRepository? deviceModelRepository = null)
    {
        _deviceRepository = deviceRepository;
        _customerRepository = customerRepository;
        _deviceModelRepository = deviceModelRepository;
    }

    public async Task<DeviceResponse> Handle(CreateDeviceCommand request, CancellationToken cancellationToken)
    {
        _ = await _customerRepository.GetByIdAsync(request.CustomerId)
            ?? throw new KeyNotFoundException($"Không tìm thấy khách hàng với Id '{request.CustomerId}'.");

        if (!Enum.TryParse<DeviceType>(request.DeviceType, true, out var deviceType))
            throw new ArgumentException($"DeviceType '{request.DeviceType}' không hợp lệ.");

        var brandName = request.Brand;
        var modelName = request.Model;
        Guid? resolvedModelId = request.DeviceModelId;

        if (_deviceModelRepository is not null)
        {
            if (request.DeviceModelId.HasValue)
            {
                var catalogModel = await _deviceModelRepository.GetByIdAsync(request.DeviceModelId.Value)
                    ?? throw new NotFoundException("Dòng máy", request.DeviceModelId.Value);

                if (!catalogModel.IsActive || !catalogModel.Brand.IsActive)
                {
                    throw new DomainException("Dòng máy hoặc hãng sản xuất đang ngưng hoạt động.");
                }

                if (catalogModel.DeviceType != deviceType)
                {
                    throw new DomainException("Dòng máy không thuộc loại thiết bị đã chọn.");
                }

                brandName = catalogModel.Brand.Name;
                modelName = catalogModel.ModelName;
            }
            else
            {
                var matched = await _deviceModelRepository.FindMatchAsync(deviceType, request.Brand, request.Model);
                if (matched is { IsActive: true, Brand.IsActive: true })
                {
                    resolvedModelId = matched.Id;
                    brandName = matched.Brand.Name;
                    modelName = matched.ModelName;
                }
            }
        }

        var device = new Device(
            request.CustomerId,
            deviceType,
            brandName,
            modelName,
            request.SerialNumber,
            resolvedModelId);

        await _deviceRepository.AddAsync(device);
        await _deviceRepository.SaveChangesAsync();

        return new DeviceResponse(
            device.Id,
            device.CustomerId,
            device.DeviceType.ToString(),
            device.Brand,
            device.Model,
            device.SerialNumber,
            device.CreatedAt,
            false,
            device.DeviceModelId);
    }
}
