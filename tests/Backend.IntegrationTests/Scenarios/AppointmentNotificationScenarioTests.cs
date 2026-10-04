using System.Net.Http.Json;
using System.Text.Json;
using RepairShop.IntegrationTests.TestDoubles;
using RepairShop.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace RepairShop.IntegrationTests.Scenarios;

[Collection(nameof(IntegrationTestCollection))]
public class AppointmentNotificationScenarioTests
{
    private readonly CustomWebApplicationFactory _factory;
    public AppointmentNotificationScenarioTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task CreatingAppointment_NotifiesAllActiveReceptionists()
    {
        var admin = await TestUserSeeder.SeedUserAsync(_factory.Services, "Admin", "adminNotif");
        var receptionist1 = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recepNotif1");
        var receptionist2 = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recepNotif2");

        var client = _factory.CreateClient();
        client.AuthorizeAs(admin.Token);
        var slotRes = await client.PostAsJsonAsync("/api/time-slots",
            new { dayOfWeek = (int?)null, slotStart = "09:00:00", slotEnd = "10:00:00", maxCapacity = 5 });
        var slotId = (await slotRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        var anonClient = _factory.CreateClient();
        await anonClient.PostAsJsonAsync("/api/appointments", new
        {
            fullName = "Test Notif",
            phone = "0933333333",
            deviceInfo = (object?)null,
            appointmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            timeSlotId = slotId,
            customerId = (Guid?)null,
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var notif1 = await db.Notifications.AnyAsync(n => n.UserId == receptionist1.UserId && n.Type == "AppointmentCreated");
        var notif2 = await db.Notifications.AnyAsync(n => n.UserId == receptionist2.UserId && n.Type == "AppointmentCreated");

        notif1.Should().BeTrue();
        notif2.Should().BeTrue(); // cả 2 Receptionist đều nhận được, không chỉ 1
    }

    [Fact]
    public async Task ConfirmingAppointment_NotifiesLinkedCustomer_NotGuestWithoutAccount()
    {
        var admin = await TestUserSeeder.SeedUserAsync(_factory.Services, "Admin", "adminNotif2");
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recepNotif3");
        var customer = await TestUserSeeder.SeedUserAsync(_factory.Services, "Customer", "custNotif");

        var client = _factory.CreateClient();
        client.AuthorizeAs(admin.Token);
        var slotRes = await client.PostAsJsonAsync("/api/time-slots",
            new { dayOfWeek = (int?)null, slotStart = "10:00:00", slotEnd = "11:00:00", maxCapacity = 5 });
        var slotId = (await slotRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        client.AuthorizeAs(receptionist.Token);
        var custRes = await client.PostAsJsonAsync("/api/customers", new
        { fullName = "Test Notif Customer", phone = "0944444444", email = (string?)null, address = (string?)null, userId = customer.UserId });
        var customerId = (await custRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        var apptRes = await client.PostAsJsonAsync("/api/appointments", new
        {
            fullName = "Test Notif Customer",
            phone = "0944444444",
            deviceInfo = (object?)null,
            appointmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            timeSlotId = slotId,
            customerId,
        });
        var appointmentId = (await apptRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        await client.PatchAsync($"/api/appointments/{appointmentId}/confirm", null);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var customerNotif = await db.Notifications.AnyAsync(n =>
            n.UserId == customer.UserId && n.Type == "AppointmentConfirmed");

        customerNotif.Should().BeTrue();
    }
}