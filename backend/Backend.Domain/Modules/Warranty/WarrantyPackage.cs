using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Exceptions;

namespace RepairShop.Domain.Modules.Warranty;

public class WarrantyPackage : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public int DurationMonths { get; private set; }
    public string Terms { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;

    private WarrantyPackage() { }

    public WarrantyPackage(string name, string? description, int durationMonths, string terms)
    {
        UpdateInfo(name, description, durationMonths, terms);
    }

    public void UpdateInfo(string name, string? description, int durationMonths, string terms)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Tên gói bảo hành không được để trống.");
        if (durationMonths <= 0) throw new DomainException("Thời hạn bảo hành phải lớn hơn 0 tháng.");
        if (string.IsNullOrWhiteSpace(terms)) throw new DomainException("Điều khoản bảo hành không được để trống.");

        Name = name.Trim();
        Description = description?.Trim();
        DurationMonths = durationMonths;
        Terms = terms.Trim();
        MarkUpdated();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        MarkUpdated();
    }
}
