using Microsoft.EntityFrameworkCore;
using System.Globalization;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Modules.SLA;

namespace RepairShop.Infrastructure.Persistence.Seeders;

public sealed class SlaPolicySeeder(AppDbContext context) : IDataSeeder
{
    public int Order => 30;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingKeys = (await context.SLAPolicies
                .Select(policy => new { policy.StatusCode, policy.DeviceType })
                .ToListAsync(cancellationToken))
            .Select(policy => BuildKey(policy.StatusCode, policy.DeviceType))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var policies = SeedData()
            .Where(data => existingKeys.Add(BuildKey(data.StatusCode, data.DeviceType)))
            .ToList();

        if (policies.Count == 0)
            return;

        foreach (var data in policies)
        {
            var policy = new SLAPolicy(data.StatusCode, data.DeviceType, data.DurationMinutes);
            context.SLAPolicies.Add(policy);
            context.Entry(policy).Property(item => item.Id).CurrentValue = data.Id;
            context.Entry(policy).Property(item => item.CreatedAt).CurrentValue = data.CreatedAt;
            context.Entry(policy).Property(item => item.UpdatedAt).CurrentValue = data.UpdatedAt;
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    private static string BuildKey(string statusCode, DeviceType? deviceType)
        => $"{statusCode}:{deviceType?.ToString() ?? "DEFAULT"}";

    private static IEnumerable<SlaSeedData> SeedData()
    {
        yield return Create("22cc50ab-5021-4c82-a4d6-b00b359380b6", "CHECKED_IN", DeviceType.Phone, 1440, "2026-11-04 12:25:06.691159+07");
        yield return Create("3f1a9b2c-7d4e-4a1b-8c3d-1a2b3c4d5e6f", "ASSIGNED", DeviceType.Laptop, 120, "2026-11-04 08:15:00+07", "2026-11-04 09:00:00+07");
        yield return Create("5e2b8c3d-9a1f-4b2c-9d4e-2b3c4d5e6f7a", "DIAGNOSING", DeviceType.Electronics, 240, "2026-11-04 09:30:00+07", "2026-11-04 10:15:00+07");
        yield return Create("7a3c1d4e-2b5f-4c3d-ae5f-3c4d5e6f7a8b", "WAITING_APPROVAL", DeviceType.Tablet, 60, "2026-11-04 10:00:00+07", "2026-11-04 10:30:00+07");
        yield return Create("8b4d2e5f-3c6a-4d4e-bf6a-4d5e6f7a8b9c", "ON_HOLD", DeviceType.Smartwatch, 480, "2026-11-04 07:00:00+07", "2026-11-04 11:00:00+07");
        yield return Create("9c5e3f6a-4d7b-4e5f-ca7b-5e6f7a8b9c0d", "WAITING_PARTS", DeviceType.Phone, 2880, "2026-11-03 14:20:00+07", "2026-11-04 08:00:00+07");
        yield return Create("1d6f4a7b-5e8c-4f6a-db8c-6f7a8b9c0d1e", "IN_REPAIR", DeviceType.Laptop, 180, "2026-11-04 11:00:00+07", "2026-11-04 12:00:00+07");
        yield return Create("2e7a5b8c-6f9d-4a7b-ec9d-7a8b9c0d1e2f", "QA_TESTING", DeviceType.Electronics, 90, "2026-11-04 12:00:00+07", "2026-11-04 13:00:00+07");
        yield return Create("3f8b6c9d-7a0e-4b8c-fd0e-8b9c0d1e2f3a", "READY_FOR_PICKUP", DeviceType.Tablet, 30, "2026-11-04 13:00:00+07", "2026-11-04 13:15:00+07");

        yield return Create("6c1e9f2a-0d3b-4e1f-2a3b-1e2f3a4b5c6d", "CHECKED_IN", null, 1440, "2026-11-04 06:30:00+07");
        yield return Create("7d2f0a3b-1e4c-4f2a-3b4c-2f3a4b5c6d7e", "ASSIGNED", null, 100, "2026-11-04 08:45:00+07", "2026-11-04 09:30:00+07");
        yield return Create("8e3a1b4c-2f5d-4a3b-4c5d-3a4b5c6d7e8f", "DIAGNOSING", null, 300, "2026-11-04 09:15:00+07", "2026-11-04 10:45:00+07");
        yield return Create("9f4b2c5d-3a6e-4b4c-5d6e-4b5c6d7e8f9a", "WAITING_APPROVAL", null, 45, "2026-11-04 10:20:00+07", "2026-11-04 10:45:00+07");
        yield return Create("0a5c3d6e-4b7f-4c5d-6e7f-5c6d7e8f9a0b", "ON_HOLD", null, 720, "2026-11-03 12:00:00+07", "2026-11-04 07:00:00+07");
        yield return Create("1b6d4e7f-5c8a-4d6e-7f8a-6d7e8f9a0b1c", "WAITING_PARTS", null, 4320, "2026-11-01 10:00:00+07", "2026-11-04 09:00:00+07");
        yield return Create("2c7e5f8a-6d9b-4e7f-8a9b-7e8f9a0b1c2d", "IN_REPAIR", null, 150, "2026-11-04 11:30:00+07", "2026-11-04 12:30:00+07");
        yield return Create("3d8f6a9b-7e0c-4f8a-9b0c-8f9a0b1c2d3e", "QA_TESTING", null, 75, "2026-11-04 12:30:00+07", "2026-11-04 13:15:00+07");
        yield return Create("4e9a7b0c-8f1d-4a9b-0c1d-9a0b1c2d3e4f", "READY_FOR_PICKUP", null, 25, "2026-11-04 13:30:00+07", "2026-11-04 13:45:00+07");
    }

    private static SlaSeedData Create(
        string id,
        string statusCode,
        DeviceType? deviceType,
        int durationMinutes,
        string createdAt,
        string? updatedAt = null)
        => new(
            Guid.Parse(id),
            statusCode,
            deviceType,
            durationMinutes,
            DateTimeOffset.Parse(createdAt, CultureInfo.InvariantCulture).UtcDateTime,
            updatedAt is null
                ? null
                : DateTimeOffset.Parse(updatedAt, CultureInfo.InvariantCulture).UtcDateTime);

    private sealed record SlaSeedData(
        Guid Id,
        string StatusCode,
        DeviceType? DeviceType,
        int DurationMinutes,
        DateTime CreatedAt,
        DateTime? UpdatedAt);
}
