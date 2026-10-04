using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RepairShop.IntegrationTests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace RepairShop.IntegrationTests.Scenarios;

[Collection(nameof(IntegrationTestCollection))]
public class AppointmentConcurrencyScenarioTests
{
    private readonly CustomWebApplicationFactory _factory;
    public AppointmentConcurrencyScenarioTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task ConcurrentBookings_NeverExceedCapacity()
    {
        var admin = await TestUserSeeder.SeedUserAsync(_factory.Services, "Admin", "adminSlot");
        var client = _factory.CreateClient();
        client.AuthorizeAs(admin.Token);

        // Tạo slot capacity = 2
        var slotRes = await client.PostAsJsonAsync("/api/time-slots",
            new { dayOfWeek = (int?)null, slotStart = "09:00:00", slotEnd = "10:00:00", maxCapacity = 2 });
        var slotId = (await slotRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        // 5 request đặt lịch CÙNG LÚC vào CÙNG 1 slot chỉ có 2 chỗ
        var tasks = Enumerable.Range(0, 5).Select(i =>
        {
            var anonClient = _factory.CreateClient();
            return anonClient.PostAsJsonAsync("/api/appointments", new
            {
                fullName = $"Khach {i}",
                phone = $"09{i}0000000",
                deviceInfo = (object?)null,
                appointmentDate = date,
                timeSlotId = slotId,
                customerId = (Guid?)null,
            });
        });

        var results = await Task.WhenAll(tasks);

        var successCount = results.Count(r => r.StatusCode == HttpStatusCode.Created);
        var fullCount = results.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        successCount.Should().Be(2); // ĐÚNG bằng capacity, không hơn, dù 5 request bắn đồng thời
        fullCount.Should().Be(3);
    }

    [Fact]
    public async Task InactiveTimeSlot_BookingRejected()
    {
        var admin = await TestUserSeeder.SeedUserAsync(_factory.Services, "Admin", "adminSlot2");
        var client = _factory.CreateClient();
        client.AuthorizeAs(admin.Token);

        var slotRes = await client.PostAsJsonAsync("/api/time-slots",
            new { dayOfWeek = (int?)null, slotStart = "11:00:00", slotEnd = "12:00:00", maxCapacity = 5 });
        var slotId = (await slotRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();
        await client.DeleteAsync($"/api/time-slots/{slotId}"); // Deactivate

        var anonClient = _factory.CreateClient();
        var res = await anonClient.PostAsJsonAsync("/api/appointments", new
        {
            fullName = "Test",
            phone = "0900000000",
            deviceInfo = (object?)null,
            appointmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            timeSlotId = slotId,
            customerId = (Guid?)null,
        });

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConfirmingAlreadyRejectedAppointment_IsRejected()
    {
        var admin = await TestUserSeeder.SeedUserAsync(_factory.Services, "Admin", "adminSlot3");
        var client = _factory.CreateClient();
        client.AuthorizeAs(admin.Token);

        var slotRes = await client.PostAsJsonAsync("/api/time-slots",
            new { dayOfWeek = (int?)null, slotStart = "13:00:00", slotEnd = "14:00:00", maxCapacity = 5 });
        var slotId = (await slotRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        var apptRes = await client.PostAsJsonAsync("/api/appointments", new
        {
            fullName = "Test Invalid",
            phone = "0911111111",
            deviceInfo = (object?)null,
            appointmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            timeSlotId = slotId,
            customerId = (Guid?)null,
        });
        var appointmentId = (await apptRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        (await client.PatchAsync($"/api/appointments/{appointmentId}/reject", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        // Thử Confirm sau khi đã Reject — phải bị chặn (checklist: "không confirm appointment đã Cancelled/Rejected")
        var confirmRes = await client.PatchAsync($"/api/appointments/{appointmentId}/confirm", null);
        confirmRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConvertingSameAppointmentTwice_IsRejected()
    {
        var admin = await TestUserSeeder.SeedUserAsync(_factory.Services, "Admin", "adminSlot4");
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recepSlot4");
        var client = _factory.CreateClient();
        client.AuthorizeAs(admin.Token);

        var slotRes = await client.PostAsJsonAsync("/api/time-slots",
            new { dayOfWeek = (int?)null, slotStart = "15:00:00", slotEnd = "16:00:00", maxCapacity = 5 });
        var slotId = (await slotRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        client.AuthorizeAs(receptionist.Token);
        var custRes = await client.PostAsJsonAsync("/api/customers", new
        { fullName = "Test Convert", phone = "0922222222", email = (string?)null, address = (string?)null, userId = (Guid?)null });
        var customerId = (await custRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();
        var devRes = await client.PostAsJsonAsync("/api/devices",
            new { customerId, deviceType = "Phone", brand = "Test", model = "X", serialNumber = $"SN-{Guid.NewGuid():N}"[..15] });
        var deviceId = (await devRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        var apptRes = await client.PostAsJsonAsync("/api/appointments", new
        {
            fullName = "Test Convert",
            phone = "0922222222",
            deviceInfo = (object?)null,
            appointmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            timeSlotId = slotId,
            customerId,
        });
        var appointmentId = (await apptRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        await client.PatchAsync($"/api/appointments/{appointmentId}/confirm", null);

        var firstConvert = await client.PostAsJsonAsync($"/api/appointments/{appointmentId}/convert-to-ticket",
            new { customerId, deviceId });
        firstConvert.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondConvert = await client.PostAsJsonAsync($"/api/appointments/{appointmentId}/convert-to-ticket",
            new { customerId, deviceId });
        secondConvert.StatusCode.Should().Be(HttpStatusCode.BadRequest); // không convert lần 2
    }
}