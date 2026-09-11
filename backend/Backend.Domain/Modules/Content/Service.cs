using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Exceptions;

namespace RepairShop.Domain.Modules.Content;

public class Service : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal? BasePrice { get; private set; }
    public string? DeviceType { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Service() { } // for EF Core

    public Service(string name, string? description, decimal? basePrice, string? deviceType)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Tên dịch vụ không được để trống.");
        if (basePrice is < 0) throw new DomainException("Giá dịch vụ không thể âm.");

        Name = name;
        Description = description;
        BasePrice = basePrice;
        DeviceType = deviceType;
    }

    public void UpdateInfo(string name, string? description, decimal? basePrice, string? deviceType)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Tên dịch vụ không được để trống.");
        if (basePrice is < 0) throw new DomainException("Giá dịch vụ không thể âm.");

        Name = name;
        Description = description;
        BasePrice = basePrice;
        DeviceType = deviceType;
        MarkUpdated();
    }

    public void Publish() { IsActive = true; MarkUpdated(); }
    public void Unpublish() { IsActive = false; MarkUpdated(); }
}