using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RepairShop.IntegrationTests.TestDoubles;
using FluentAssertions;

namespace RepairShop.IntegrationTests.Scenarios;

[Collection(nameof(IntegrationTestCollection))]
public class InvalidStateTransitionScenarioTests
{
    private readonly CustomWebApplicationFactory _factory;
    public InvalidStateTransitionScenarioTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Ticket_CheckedIn_CannotJumpDirectlyTo_ReadyForPickup()
    {
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recep");

        var client = _factory.CreateClient();
        client.AuthorizeAs(receptionist.Token);

        var custRes = await client.PostAsJsonAsync("/api/customers",
            new { fullName = "Test E", phone = $"09{Random.Shared.Next(10000000, 99999999)}", email = (string?)null, address = (string?)null });
        var customerId = (await custRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        var devRes = await client.PostAsJsonAsync("/api/devices",
            new { customerId, deviceType = "Laptop", brand = "Dell", model = "XPS 13", serialNumber = $"SN-{Guid.NewGuid():N}"[..15] });
        var deviceId = (await devRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        var ticketRes = await client.PostAsJsonAsync("/api/tickets",
            new { customerId, deviceId, issueDescription = "Không lên nguồn", notes = (string?)null, conditionNotes = (string?)null, riskWarning = (string?)null });
        var ticket = await ticketRes.ReadAsAsync<JsonElement>();
        var ticketId = ticket.GetProperty("id").GetGuid();
        ticket.GetProperty("status").GetString().Should().Be("CHECKED_IN"); // ví dụ mentor: điểm xuất phát

        // Không có endpoint public nào cho phép set thẳng READY_FOR_PICKUP — thử qua đường "hợp lệ nhất"
        // có thể bị lạm dụng: gọi qa-pass khi ticket còn CHECKED_IN (chưa qua Assign/Diagnosis/Quote/Repair/QA)
        var qaPassRes = await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/qa-pass", new
        {
            functionalCheckNotes = "test",
            cosmeticCheckNotes = "test",
            originalIssueResolvedNotes = "test"
        });

        // Domain phải reject vì StartQualityCheck() đòi hỏi IN_REPAIR trước (RepairTicketStateMachine, Task 4.2)
        // -> route "nhảy cóc" duy nhất có thể thử bằng API công khai đã bị chặn ở tầng Domain.
        qaPassRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await qaPassRes.ReadAsAsync<JsonElement>();
        error.GetProperty("message").GetString().Should().Contain("IN_REPAIR");

        // Xác nhận status KHÔNG bị đổi sau lần thử thất bại — vẫn đúng CHECKED_IN
        var getTicketRes = await client.GetAsync($"/api/tickets/{ticketId}");
        (await getTicketRes.ReadAsAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("CHECKED_IN");
    }

    [Fact]
    public async Task Ticket_Assigned_CannotTransitionBackToItself_WhenReassigning()
    {
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recep");
        var technicianA = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "techA");
        var technicianB = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "techB");

        var client = _factory.CreateClient();
        client.AuthorizeAs(receptionist.Token);

        var custRes = await client.PostAsJsonAsync("/api/customers",
            new { fullName = "Test E2", phone = $"09{Random.Shared.Next(10000000, 99999999)}", email = (string?)null, address = (string?)null });
        var customerId = (await custRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();
        var devRes = await client.PostAsJsonAsync("/api/devices",
            new { customerId, deviceType = "Phone", brand = "Xiaomi", model = "13T", serialNumber = $"SN-{Guid.NewGuid():N}"[..15] });
        var deviceId = (await devRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();
        var ticketRes = await client.PostAsJsonAsync("/api/tickets",
            new { customerId, deviceId, issueDescription = "test", notes = (string?)null, conditionNotes = (string?)null, riskWarning = (string?)null });
        var ticketId = (await ticketRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/assign-technician",
            new { technicianId = technicianA.UserId, note = (string?)null }); // CHECKED_IN -> ASSIGNED

        // Assign lại lần 2 khi đã ASSIGNED — state machine chỉ cho ASSIGNED -> DIAGNOSING, không có ASSIGNED -> ASSIGNED
        var reassignRes = await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/assign-technician",
            new { technicianId = technicianB.UserId, note = (string?)null });

        reassignRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}