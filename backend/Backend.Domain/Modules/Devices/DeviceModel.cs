using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Common.Exceptions;

namespace RepairShop.Domain.Modules.Devices;

public class DeviceModel : BaseEntity
{
    public Guid BrandId { get; private set; }
    public DeviceType DeviceType { get; private set; }
    public string ModelName { get; private set; } = default!;
    public string? ModelNumber { get; private set; }
    public int? ReleaseYear { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Brand Brand { get; private set; } = default!;
    public ICollection<Device> Devices { get; private set; } = new List<Device>();

    private DeviceModel() { } // for EF Core

    public DeviceModel(
        Guid brandId,
        DeviceType deviceType,
        string modelName,
        string? modelNumber = null,
        int? releaseYear = null,
        bool isActive = true)
    {
        ValidateAndApply(brandId, deviceType, modelName, modelNumber, releaseYear);
        IsActive = isActive;
    }

    public void UpdateInfo(
        Guid brandId,
        DeviceType deviceType,
        string modelName,
        string? modelNumber,
        int? releaseYear)
    {
        ValidateAndApply(brandId, deviceType, modelName, modelNumber, releaseYear);
        MarkUpdated();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        MarkUpdated();
    }

    public void Activate() => SetActive(true);
    public void Deactivate() => SetActive(false);

    private void ValidateAndApply(
        Guid brandId,
        DeviceType deviceType,
        string modelName,
        string? modelNumber,
        int? releaseYear)
    {
        if (brandId == Guid.Empty)
            throw new DomainException("Hãng sản xuất không hợp lệ.");

        if (string.IsNullOrWhiteSpace(modelName))
            throw new DomainException("Tên dòng máy (Model) không được để trống.");

        if (releaseYear.HasValue && (releaseYear.Value < 1990 || releaseYear.Value > 2100))
            throw new DomainException("Năm phát hành không hợp lệ.");

        BrandId = brandId;
        DeviceType = deviceType;
        ModelName = modelName.Trim();
        ModelNumber = string.IsNullOrWhiteSpace(modelNumber) ? null : modelNumber.Trim();
        ReleaseYear = releaseYear;
    }
}
