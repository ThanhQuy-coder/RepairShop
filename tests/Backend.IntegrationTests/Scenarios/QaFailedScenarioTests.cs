using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RepairShop.IntegrationTests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace RepairShop.IntegrationTests.Scenarios;

[Collection(nameof(IntegrationTestCollection))]
public class QaFailedScenarioTests
{
    private readonly CustomWebApplicationFactory _factory;
    public QaFailedScenarioTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task QaFail_ShouldReturnToInRepair_ThenQaPass_ShouldSucceed()
    {
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recep");
        var technician = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "tech");
        var customer = await TestUserSeeder.SeedUserAsync(_factory.Services, "Customer", "cust");

        var client = _factory.CreateClient();
        client.AuthorizeAs(receptionist.Token);
        var (ticketId, quoteId) = await WorkflowHelpers.CreateTicketUpToQuote(client, technician.Token, customer.UserId);

        client.AuthorizeAs(customer.Token);
        await client.PatchAsync($"/api/quotes/{quoteId}/approve", null); // -> IN_REPAIR

        client.AuthorizeAs(technician.Token);
        await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/completion-notes", new { completionNotes = "Đã thay màn hình (lần 1)" });
        var startQa1 = await client.PatchAsync($"/api/tickets/{ticketId}/start-qa", null);
        startQa1.StatusCode.Should().Be(HttpStatusCode.OK);

        // FAIL — quay lại IN_REPAIR
        var failRes = await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/qa-fail", new { failureReason = "Màn hình bị ám vàng góc trên" });
        failRes.StatusCode.Should().Be(HttpStatusCode.OK);
        (await failRes.ReadAsAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("IN_REPAIR");

        // Sửa lại lần 2, cập nhật Completion Notes MỚI rồi thử QA lại
        await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/completion-notes", new { completionNotes = "Đã đổi màn hình khác (lần 2), hết ám vàng" });
        var startQa2 = await client.PatchAsync($"/api/tickets/{ticketId}/start-qa", null);
        startQa2.StatusCode.Should().Be(HttpStatusCode.OK);

        // PASS lần 2 — BR-19 vẫn thoả vì ticket ĐÃ TỪNG ở IN_REPAIR (nhiều lần), không quan trọng lần thứ mấy
        var passRes = await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/qa-pass", new
        {
            functionalCheckNotes = "Hiển thị bình thường", cosmeticCheckNotes = "Không còn ám vàng",
            originalIssueResolvedNotes = "Đã khắc phục hoàn toàn"
        });
        passRes.StatusCode.Should().Be(HttpStatusCode.OK);
        (await passRes.ReadAsAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("READY_FOR_PICKUP");

        // Status History phải ghi nhận ĐỦ chu kỳ IN_REPAIR -> QA_TESTING -> IN_REPAIR -> QA_TESTING -> READY_FOR_PICKUP
        var historyRes = await client.GetAsync($"/api/tickets/{ticketId}/status-history");
        var codes = (await historyRes.ReadAsAsync<JsonElement>()).EnumerateArray()
            .Select(h => h.GetProperty("toStatus").GetString()).ToList();
        codes.Should().ContainInOrder("IN_REPAIR", "QA_TESTING", "IN_REPAIR", "QA_TESTING", "READY_FOR_PICKUP");
    }
}