using RepairShop.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace RepairShop.Infrastructure.Time;

public sealed class BusinessClock(IConfiguration configuration) : IBusinessClock
{
    private readonly TimeZoneInfo _timeZone = TimeZoneInfo.FindSystemTimeZoneById(
        configuration["Appointment:TimeZone"] ?? "SE Asia Standard Time");

    public DateTimeOffset Now => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _timeZone);
    public TimeZoneInfo TimeZone => _timeZone;
}
