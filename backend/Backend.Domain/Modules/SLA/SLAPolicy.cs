using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Common.Exceptions;

namespace RepairShop.Domain.Modules.SLA;

public sealed class SLAPolicy : BaseEntity
{
    public string StatusCode { get; private set; } = default!;
    public DeviceType? DeviceType { get; private set; }
    public int DurationMinutes { get; private set; }
    public bool IsActive { get; private set; }

    private SLAPolicy() { }

    public SLAPolicy(string statusCode, DeviceType? deviceType, int durationMinutes)
    {
        if (string.IsNullOrWhiteSpace(statusCode))
            throw new DomainException("StatusCode không được để trống.");
        if (durationMinutes <= 0)
            throw new DomainException("Thời lượng SLA phải lớn hơn 0.");

        StatusCode = statusCode.Trim().ToUpperInvariant();
        DeviceType = deviceType;
        DurationMinutes = durationMinutes;
        IsActive = true;
    }

    public void Update(int durationMinutes, bool isActive)
    {
        if (durationMinutes <= 0)
            throw new DomainException("Thời lượng SLA phải lớn hơn 0.");
        DurationMinutes = durationMinutes;
        IsActive = isActive;
        MarkUpdated();
    }
}
