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
    public async Task Ticket_WhenQuoteRejected_ShouldTransitionToClosedRejected_AndStayTerminal()
    {
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recep");
        var technician = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "tech");
        var customer = await TestUserSeeder.SeedUserAsync(_factory.Services, "Customer", "cust");

        var client = _factory.CreateClient();
        client.AuthorizeAs(receptionist.Token);

        var (ticketId, quoteId) = await WorkflowHelpers.CreateTicketUpToQuote(client, technician.Token, customer.UserId);

        // Reject với lý do
        client.AuthorizeAs(customer.Token);
        var rejectRes = await client.PatchAsJsonAsync($"/api/quotes/{quoteId}/reject", new { rejectReason = "Giá quá cao so với thị trường" });
        rejectRes.StatusCode.Should().Be(HttpStatusCode.OK);
        (await rejectRes.ReadAsAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("Rejected");

        // Ticket phải đi đúng nhánh CLOSED_REJECTED (Task 4.9)
        client.AuthorizeAs(receptionist.Token);
        var ticketRes = await client.GetAsync($"/api/tickets/{ticketId}");
        (await ticketRes.ReadAsAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("CLOSED_REJECTED");

        // CLOSED_REJECTED là terminal — không còn transition nào đi tiếp được (Task 4.2)
        var tryAssignAgain = await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/assign-technician",
            new { technicianId = technician.UserId, note = (string?)null });
        tryAssignAgain.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RejectQuote_WithoutReason_ShouldReturn400()
    {
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recep");
        var technician = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "tech");
        var customer = await TestUserSeeder.SeedUserAsync(_factory.Services, "Customer", "cust");

        var client = _factory.CreateClient();
        client.AuthorizeAs(receptionist.Token);
        var (_, quoteId) = await WorkflowHelpers.CreateTicketUpToQuote(client, technician.Token, customer.UserId);

        client.AuthorizeAs(customer.Token);
        var rejectRes = await client.PatchAsJsonAsync($"/api/quotes/{quoteId}/reject", new { rejectReason = "" });

        rejectRes.StatusCode.Should().Be(HttpStatusCode.BadRequest); // FluentValidation chặn (Task 4.9)
    }
}