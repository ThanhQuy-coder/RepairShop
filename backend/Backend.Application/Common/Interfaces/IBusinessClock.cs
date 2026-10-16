namespace RepairShop.Application.Common.Interfaces;

public interface IBusinessClock
{
    DateTimeOffset Now { get; }
    TimeZoneInfo TimeZone { get; }
}
