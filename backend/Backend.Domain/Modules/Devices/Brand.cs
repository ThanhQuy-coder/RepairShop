using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Exceptions;

namespace RepairShop.Domain.Modules.Devices;

public class Brand : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public string? LogoUrl { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    public ICollection<DeviceModel> DeviceModels { get; private set; } = new List<DeviceModel>();

    private Brand() { } // for EF Core

    public Brand(string name, string code, string? logoUrl = null, string? description = null, bool isActive = true)
    {
        ValidateAndApply(name, code, logoUrl, description);
        IsActive = isActive;
    }

    public void UpdateInfo(string name, string code, string? logoUrl, string? description)
    {
        ValidateAndApply(name, code, logoUrl, description);
        MarkUpdated();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        MarkUpdated();
    }

    public void Activate() => SetActive(true);
    public void Deactivate() => SetActive(false);

    private void ValidateAndApply(string name, string code, string? logoUrl, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tên hãng sản xuất không được để trống.");

        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Mã hãng sản xuất không được để trống.");

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        LogoUrl = string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
