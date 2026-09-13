using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using RepairShop.IntegrationTests;
using RepairShop.IntegrationTests.TestDoubles;

[Collection(nameof(IntegrationTestCollection))]
public class StateMachineEnforcementProofTests
{
    private readonly CustomWebApplicationFactory _factory;
    public StateMachineEnforcementProofTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DirectApiCall_CannotBypassStateMachine_EvenWithValidAuth()
    {
        // Chứng minh: dù có đúng quyền (Technician), gọi thẳng API mà bỏ qua bước trung gian
        // (VD gọi qa-pass khi ticket còn ASSIGNED) vẫn bị Domain chặn — không phải do Controller
        // kiểm tra "đã qua bước X chưa" một cách thủ công, mà do RepairTicketStateMachine.
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recepSM");
        var technician = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "techSM");

        var client = _factory.CreateClient();
        client.AuthorizeAs(receptionist.Token);

        var custRes = await client.PostAsJsonAsync("/api/customers", new
        { fullName = "Test SM", phone = $"09{Random.Shared.Next(10000000, 99999999)}", email = (string?)null, address = (string?)null, userId = (Guid?)null });
        var customerId = (await custRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();
        var devRes = await client.PostAsJsonAsync("/api/devices",
            new { customerId, deviceType = "Phone", brand = "Test", model = "X", serialNumber = $"SN-{Guid.NewGuid():N}"[..15] });
        var deviceId = (await devRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();
        var ticketRes = await client.PostAsJsonAsync("/api/tickets",
            new { customerId, deviceId, issueDescription = "test", notes = (string?)null, conditionNotes = (string?)null, riskWarning = (string?)null });
        var ticketId = (await ticketRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/assign-technician", new { technicianId = technician.UserId, note = (string?)null });

        client.AuthorizeAs(technician.Token);
        // Ticket đang ở ASSIGNED — gọi thẳng qa-pass (nhảy qua Diagnosis/Quote/Repair/QA_TESTING)
        var qaPassRes = await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/qa-pass", new
        { functionalCheckNotes = "hack", cosmeticCheckNotes = "hack", originalIssueResolvedNotes = "hack" });

        qaPassRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Xác nhận status KHÔNG bị đổi
        client.AuthorizeAs(receptionist.Token);
        var checkRes = await client.GetAsync($"/api/tickets/{ticketId}");
        (await checkRes.ReadAsAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("ASSIGNED");
    }
}