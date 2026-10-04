using RepairShop.Domain.Common.Exceptions;

namespace RepairShop.Domain.Modules.Appointments;

public class TimeSlotConfig
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public int? DayOfWeek { get; private set; }
    public TimeOnly SlotStart { get; private set; }
    public TimeOnly SlotEnd { get; private set; }
    public int MaxCapacity { get; private set; }
    public bool IsActive { get; private set; } = true;

    private TimeSlotConfig() { }

    public TimeSlotConfig(TimeOnly slotStart, TimeOnly slotEnd, int maxCapacity, int? dayOfWeek = null)
    {
        if (dayOfWeek is < 0 or > 6)
            throw new DomainException("DayOfWeek phải từ 0 (Chủ nhật) đến 6 (Thứ 7), hoặc để trống áp dụng mọi ngày.");
        if (slotEnd <= slotStart)
            throw new DomainException("SlotEnd phải sau SlotStart.");
        if (maxCapacity <= 0)
            throw new DomainException("MaxCapacity phải lớn hơn 0.");

        DayOfWeek = dayOfWeek;
        SlotStart = slotStart;
        SlotEnd = slotEnd;
        MaxCapacity = maxCapacity;
    }

    public void UpdateInfo(TimeOnly slotStart, TimeOnly slotEnd, int maxCapacity, int? dayOfWeek)
    {
        if (dayOfWeek is < 0 or > 6)
            throw new DomainException("DayOfWeek phải từ 0 đến 6, hoặc để trống.");
        if (slotEnd <= slotStart)
            throw new DomainException("SlotEnd phải sau SlotStart.");
        if (maxCapacity <= 0)
            throw new DomainException("MaxCapacity phải lớn hơn 0.");

        DayOfWeek = dayOfWeek;
        SlotStart = slotStart;
        SlotEnd = slotEnd;
        MaxCapacity = maxCapacity;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    /// <summary>Kiểm tra slot này có áp dụng cho 1 ngày cụ thể hay không (khớp DayOfWeek hoặc null = mọi ngày).</summary>
    public bool AppliesTo(DateOnly date) =>
        IsActive && (DayOfWeek is null || DayOfWeek == (int)date.DayOfWeek);
}