using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RepairShop.IntegrationTests.TestDoubles;
using FluentAssertions;

namespace RepairShop.IntegrationTests.Scenarios;

[Collection(nameof(IntegrationTestCollection))]
public class QuoteRejectedScenarioTests
{
    private readonly CustomWebApplicationFactory _factory;
    public QuoteRejectedScenarioTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Quote_WhenRejected_ShouldRemainVersionableForRequote()
    {
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recep");
        var technician = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "tech");
        var customer = await TestUserSeeder.SeedUserAsync(_factory.Services, "Customer", "cust");

        var client = _factory.CreateClient();
        client.AuthorizeAs(receptionist.Token);

        var (ticketId, quoteId) = await WorkflowHelpers.CreateTicketUpToQuote(
            client, technician.Token, technician.UserId, customer.UserId, customer.CustomerId);
        // Reject với lý do
        client.AuthorizeAs(customer.Token);
        var rejectRes = await client.PatchAsJsonAsync($"/api/quotes/{quoteId}/reject", new { rejectReason = "Giá quá cao so với thị trường" });
        rejectRes.StatusCode.Should().Be(HttpStatusCode.OK);
        (await rejectRes.ReadAsAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("QuoteRejected");

        // Từ chối quote kết thúc phiên bản quote hiện tại, không đóng vĩnh viễn ticket.
        client.AuthorizeAs(receptionist.Token);
        var ticketRes = await client.GetAsync($"/api/tickets/{ticketId}");
        (await ticketRes.ReadAsAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("WAITING_APPROVAL");
    }

    [Fact]
    public async Task RejectQuote_WithoutReason_ShouldReturn400()
    {
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recep");
        var technician = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "tech");
        var customer = await TestUserSeeder.SeedUserAsync(_factory.Services, "Customer", "cust");

        var client = _factory.CreateClient();
        client.AuthorizeAs(receptionist.Token);
        var (_, quoteId) = await WorkflowHelpers.CreateTicketUpToQuote(client, technician.Token, technician.UserId, customer.UserId, customer.CustomerId);

        client.AuthorizeAs(customer.Token);
        var rejectRes = await client.PatchAsJsonAsync($"/api/quotes/{quoteId}/reject", new { rejectReason = "" });

        rejectRes.StatusCode.Should().Be(HttpStatusCode.BadRequest); // FluentValidation chặn (Task 4.9)
    }
}