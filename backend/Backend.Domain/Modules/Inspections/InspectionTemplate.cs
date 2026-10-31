using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Enums;
using System.Security.Cryptography;
using System.Text;

namespace RepairShop.Domain.Modules.Inspections;

public class InspectionTemplate : BaseEntity
{
    public string Name { get; private set; } = default!;
    public DeviceType? DeviceType { get; private set; }
    public InspectionType InspectionType { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    private InspectionTemplate() { }

    public InspectionTemplate(string name, DeviceType? deviceType, InspectionType inspectionType, int sortOrder)
    {
        Id = new Guid(MD5.HashData(Encoding.UTF8.GetBytes($"{inspectionType}:{deviceType}:{name}")));
        Name = name;
        DeviceType = deviceType;
        InspectionType = inspectionType;
        SortOrder = sortOrder;
    }
}
