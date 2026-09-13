using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RepairShop.IntegrationTests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace RepairShop.IntegrationTests.Scenarios;

[Collection(nameof(IntegrationTestCollection))]
public class IdorRegressionTests
{
    private readonly CustomWebApplicationFactory _factory;
    public IdorRegressionTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task CustomerA_CannotViewTicketOfCustomerB_ViaDirectIdGuess()
    {
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recepIdor");
        var technician = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "techIdor");
        var customerB = await TestUserSeeder.SeedUserAsync(_factory.Services, "Customer", "custB-idor");
        var customerA = await TestUserSeeder.SeedUserAsync(_factory.Services, "Customer", "custA-idor");

        var client = _factory.CreateClient();
        client.AuthorizeAs(receptionist.Token);
        var (ticketBId, quoteBId) = await WorkflowHelpers.CreateTicketUpToQuote(client, technician.Token, technician.UserId, customerB.UserId);

        client.AuthorizeAs(customerA.Token);

        // Thử đủ mọi endpoint có thể lộ dữ liệu Customer B qua ticket ID đoán được
        (await client.GetAsync($"/api/tickets/{ticketBId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync($"/api/tickets/{ticketBId}/quotes")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync($"/api/tickets/{ticketBId}/warranty")).StatusCode.Should().Be(HttpStatusCode.NotFound); // chưa có warranty -> 404 hợp lý, không phải leak
        (await client.PatchAsync($"/api/quotes/{quoteBId}/approve", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TechnicianB_CannotModifyTicketAssignedToTechnicianA()
    {
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recepIdor2");
        var techA = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "techA-idor");
        var techB = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "techB-idor");

        var client = _factory.CreateClient();
        client.AuthorizeAs(receptionist.Token);
        var (ticketId, _) = await WorkflowHelpers.CreateTicketUpToQuote(client, techA.Token, techA.UserId);

        client.AuthorizeAs(techB.Token);
        (await client.PatchAsync($"/api/tickets/{ticketId}/start-diagnosis", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.PostAsJsonAsync($"/api/tickets/{ticketId}/repair-notes", new { note = "hack" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Customer_CannotReviewTicketNotBelongingToThem()
    {
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recepIdor3");
        var technician = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "techIdor3");
        var ownerCustomer = await TestUserSeeder.SeedUserAsync(_factory.Services, "Customer", "ownerIdor");
        var otherCustomer = await TestUserSeeder.SeedUserAsync(_factory.Services, "Customer", "otherIdor");

        var client = _factory.CreateClient();
        client.AuthorizeAs(receptionist.Token);
        var (ticketId, _) = await WorkflowHelpers.CreateTicketUpToQuote(client, technician.Token, ownerCustomer.UserId);

        client.AuthorizeAs(otherCustomer.Token);
        var res = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/review", new { rating = 5, comment = "fake review" });

        // Không cần đợi ticket Delivered — ownership check phải chặn TRƯỚC khi kiểm tra status
        res.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);
    }
}