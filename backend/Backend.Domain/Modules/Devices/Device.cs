using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Modules.Customers;
using RepairShop.Domain.Modules.Tickets;
using RepairShop.Domain.Common.Exceptions;


namespace RepairShop.Domain.Modules.Devices;

public class Device : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public Guid? DeviceModelId { get; private set; }
    public DeviceType DeviceType { get; private set; }
    public string Brand { get; private set; } = default!;
    public string Model { get; private set; } = default!;
    public string? SerialNumber { get; private set; } // nullable — không phải thiết bị nào cũng có IMEI

    public Customer Customer { get; private set; } = default!;
    public DeviceModel? DeviceModel { get; private set; }
    public ICollection<RepairTicket> RepairTickets { get; private set; } = new List<RepairTicket>();

    private Device() { } // for EF Core

    public Device(Guid customerId, DeviceType deviceType, string brand, string model, string? serialNumber = null, Guid? deviceModelId = null)
    {
        if (string.IsNullOrWhiteSpace(brand))
            throw new DomainException("Thương hiệu thiết bị không được để trống.");

        if (string.IsNullOrWhiteSpace(model))
            throw new DomainException("Model thiết bị không được để trống.");

        CustomerId = customerId;
        DeviceModelId = deviceModelId;
        DeviceType = deviceType;
        Brand = brand.Trim();
        Model = model.Trim();
        SerialNumber = string.IsNullOrWhiteSpace(serialNumber) ? null : serialNumber.Trim();
    }

    public void UpdateInfo(string brand, string model, string? serialNumber, Guid? deviceModelId = null)
    {
        if (string.IsNullOrWhiteSpace(brand))
            throw new DomainException("Thương hiệu thiết bị không được để trống.");

        if (string.IsNullOrWhiteSpace(model))
            throw new DomainException("Model thiết bị không được để trống.");

        Brand = brand.Trim();
        Model = model.Trim();
        SerialNumber = string.IsNullOrWhiteSpace(serialNumber) ? null : serialNumber.Trim();
        if (deviceModelId.HasValue)
        {
            DeviceModelId = deviceModelId;
        }
        MarkUpdated();
    }

    public void LinkModel(Guid? deviceModelId)
    {
        DeviceModelId = deviceModelId;
        MarkUpdated();
    }
}
