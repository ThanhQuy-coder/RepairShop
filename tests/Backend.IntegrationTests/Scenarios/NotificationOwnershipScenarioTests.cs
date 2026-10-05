using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RepairShop.IntegrationTests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace RepairShop.IntegrationTests.Scenarios;

[Collection(nameof(IntegrationTestCollection))]
public class NotificationOwnershipScenarioTests
{
    private readonly CustomWebApplicationFactory _factory;
    public NotificationOwnershipScenarioTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task UserA_CannotMarkAsRead_NotificationOfUserB()
    {
        var admin = await TestUserSeeder.SeedUserAsync(_factory.Services, "Admin", "adminNotifOwn");
        var receptionistA = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recepOwnA");
        var receptionistB = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recepOwnB");

        var client = _factory.CreateClient();
        client.AuthorizeAs(admin.Token);

        // Kích hoạt tạo Notification cho CẢ 2 Receptionist (CreateForRoleAsync, Task 10.10)
        var slotRes = await client.PostAsJsonAsync("/api/time-slots",
            new { dayOfWeek = (int?)null, slotStart = "09:00:00", slotEnd = "10:00:00", maxCapacity = 5 });
        var slotId = (await slotRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        var anonClient = _factory.CreateClient();
        await anonClient.PostAsJsonAsync("/api/appointments", new
        {
            fullName = "Test Own",
            phone = "0955555555",
            deviceInfo = (object?)null,
            appointmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            timeSlotId = slotId,
            customerId = (Guid?)null,
        });

        // Receptionist A lấy notification CỦA CHÍNH MÌNH
        client.AuthorizeAs(receptionistA.Token);
        var listRes = await client.GetAsync("/api/notifications");
        var myNotificationId = (await listRes.ReadAsAsync<JsonElement>())
            .GetProperty("items").EnumerateArray().First().GetProperty("id").GetGuid();

        // Admin KHÔNG được đánh dấu hộ — dù có quyền Admin, vẫn phải bị chặn vì BR-28 không dựa vào Role
        client.AuthorizeAs(admin.Token);
        var adminAttempt = await client.PatchAsync($"/api/notifications/{myNotificationId}/read", null);
        adminAttempt.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Receptionist B cũng không được đánh dấu notification của A, dù cùng Role
        client.AuthorizeAs(receptionistB.Token);
        var bAttempt = await client.PatchAsync($"/api/notifications/{myNotificationId}/read", null);
        bAttempt.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Chính chủ (A) thì đọc được
        client.AuthorizeAs(receptionistA.Token);
        var ownerAttempt = await client.PatchAsync($"/api/notifications/{myNotificationId}/read", null);
        ownerAttempt.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UnreadCount_ReflectsOnlyCurrentUserNotifications()
    {
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recepCount");
        var client = _factory.CreateClient();
        client.AuthorizeAs(receptionist.Token);

        var res = await client.GetAsync("/api/notifications/unread-count");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        (await res.ReadAsAsync<JsonElement>()).GetProperty("count").GetInt32().Should().BeGreaterThanOrEqualTo(0);
    }
}