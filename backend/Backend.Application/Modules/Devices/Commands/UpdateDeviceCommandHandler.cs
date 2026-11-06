using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Devices.DTOs;
using RepairShop.Domain.Common.Exceptions;
using MediatR;

namespace RepairShop.Application.Modules.Devices.Commands;

public class UpdateDeviceCommandHandler : IRequestHandler<UpdateDeviceCommand, DeviceResponse>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IDeviceModelRepository? _deviceModelRepository;

    public UpdateDeviceCommandHandler(
        IDeviceRepository deviceRepository,
        IDeviceModelRepository? deviceModelRepository = null)
    {
        _deviceRepository = deviceRepository;
        _deviceModelRepository = deviceModelRepository;
    }

    public async Task<DeviceResponse> Handle(UpdateDeviceCommand request, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException($"Không tìm thấy thiết bị với Id '{request.Id}'.");

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

                if (catalogModel.DeviceType != device.DeviceType)
                {
                    throw new DomainException("Dòng máy không thuộc loại thiết bị hiện tại.");
                }

                brandName = catalogModel.Brand.Name;
                modelName = catalogModel.ModelName;
            }
            else
            {
                var matched = await _deviceModelRepository.FindMatchAsync(device.DeviceType, request.Brand, request.Model);
                if (matched is { IsActive: true, Brand.IsActive: true })
                {
                    resolvedModelId = matched.Id;
                    brandName = matched.Brand.Name;
                    modelName = matched.ModelName;
                }
            }
        }

        device.UpdateInfo(brandName, modelName, request.SerialNumber, resolvedModelId);

        _deviceRepository.Update(device);
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
