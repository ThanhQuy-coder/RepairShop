using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Common.Exceptions;

namespace RepairShop.Domain.Modules.Diagnostics;

public class DiagnosisSymptom : BaseEntity
{
    public string Name { get; private set; } = default!;
    public DeviceType DeviceType { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }

    private DiagnosisSymptom() { }

    public DiagnosisSymptom(string name, DeviceType deviceType, int sortOrder)
    {
        Validate(name, sortOrder);
        Name = name.Trim();
        DeviceType = deviceType;
        SortOrder = sortOrder;
    }

    public void UpdateInfo(string name, DeviceType deviceType, int sortOrder)
    {
        Validate(name, sortOrder);
        Name = name.Trim();
        DeviceType = deviceType;
        SortOrder = sortOrder;
        MarkUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }

    private static void Validate(string name, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tên triệu chứng không được để trống.");
        if (name.Length > 200)
            throw new DomainException("Tên triệu chứng không được vượt quá 200 ký tự.");
        if (sortOrder < 0)
            throw new DomainException("Thứ tự sắp xếp không thể âm.");
    }
}
