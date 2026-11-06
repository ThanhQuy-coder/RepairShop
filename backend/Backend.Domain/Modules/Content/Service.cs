using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Exceptions;

namespace RepairShop.Domain.Modules.Content;

public class Service : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal? BasePrice { get; private set; }
    public string? DeviceType { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Service() { } // for EF Core

    public Service(string name, string? description, decimal? basePrice, string? deviceType, string? imageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Tên dịch vụ không được để trống.");
        if (basePrice is < 0) throw new DomainException("Giá dịch vụ không thể âm.");

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        BasePrice = basePrice;
        DeviceType = string.IsNullOrWhiteSpace(deviceType) ? null : deviceType.Trim();
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
    }

    public void UpdateInfo(string name, string? description, decimal? basePrice, string? deviceType, string? imageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Tên dịch vụ không được để trống.");
        if (basePrice is < 0) throw new DomainException("Giá dịch vụ không thể âm.");

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        BasePrice = basePrice;
        DeviceType = string.IsNullOrWhiteSpace(deviceType) ? null : deviceType.Trim();
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        MarkUpdated();
    }

    public void UpdateImage(string? imageUrl)
    {
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        MarkUpdated();
    }

    public void Publish() { IsActive = true; MarkUpdated(); }
    public void Unpublish() { IsActive = false; MarkUpdated(); }
}
