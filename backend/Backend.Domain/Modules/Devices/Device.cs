using Backend.Domain.Common;
using Backend.Domain.Common.Enums;
using Backend.Domain.Modules.Customers;
using Backend.Domain.Modules.Tickets;

namespace Backend.Domain.Modules.Devices;

public class Device : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public DeviceType DeviceType { get; private set; }
    public string Brand { get; private set; } = default!;
    public string Model { get; private set; } = default!;
    public string? SerialNumber { get; private set; } // nullable — không phải thiết bị nào cũng có IMEI

    public Customer Customer { get; private set; } = default!;
    public ICollection<RepairTicket> RepairTickets { get; private set; } = new List<RepairTicket>();

    private Device() { } // for EF Core

    public Device(Guid customerId, DeviceType deviceType, string brand, string model, string? serialNumber = null)
    {
        if (string.IsNullOrWhiteSpace(brand))
            throw new DomainException("Thương hiệu thiết bị không được để trống.");

        if (string.IsNullOrWhiteSpace(model))
            throw new DomainException("Model thiết bị không được để trống.");

        CustomerId = customerId;
        DeviceType = deviceType;
        Brand = brand;
        Model = model;
        SerialNumber = serialNumber;
    }

    public void UpdateInfo(string brand, string model, string? serialNumber)
    {
        Brand = brand;
        Model = model;
        SerialNumber = serialNumber;
        MarkUpdated();
    }
}