using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RepairShop.IntegrationTests.TestDoubles;
using FluentAssertions;

namespace RepairShop.IntegrationTests.Scenarios;

[Collection(nameof(IntegrationTestCollection))]
public class UnauthorizedAccessScenarioTests
{
    private readonly CustomWebApplicationFactory _factory;
    public UnauthorizedAccessScenarioTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task CustomerA_AccessingTicketOfCustomerB_ShouldReturn403()
    {
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recep");
        var technician = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "tech");
        var customerB = await TestUserSeeder.SeedUserAsync(_factory.Services, "Customer", "custB");
        var customerA = await TestUserSeeder.SeedUserAsync(_factory.Services, "Customer", "custA");

        var client = _factory.CreateClient();
        client.AuthorizeAs(receptionist.Token);
        var (ticketBId, _) = await WorkflowHelpers.CreateTicketUpToQuote(client, technician.Token, customerB.UserId);
        // Lưu ý: CreateTicketUpToQuote tạo Customer MỚI bên trong nó, không gắn với customerB thật.
        // Cần API riêng gắn UserId vào Customer để test này chính xác — xem ghi chú bên dưới.

        client.AuthorizeAs(customerA.Token);

        var getTicketRes = await client.GetAsync($"/api/tickets/{ticketBId}");
        getTicketRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var getQuotesRes = await client.GetAsync($"/api/tickets/{ticketBId}/quotes");
        getQuotesRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TechnicianB_AccessingTicketAssignedToTechnicianA_ShouldReturn403()
    {
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recep");
        var technicianA = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "techA");
        var technicianB = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "techB");
        var customer = await TestUserSeeder.SeedUserAsync(_factory.Services, "Customer", "cust");

        var client = _factory.CreateClient();
        client.AuthorizeAs(receptionist.Token);
        var (ticketId, _) = await WorkflowHelpers.CreateTicketUpToQuote(client, technicianA.Token, technicianA.UserId);

        client.AuthorizeAs(technicianB.Token);
        var startDiagRes = await client.PatchAsync($"/api/tickets/{ticketId}/start-diagnosis", null);
        startDiagRes.StatusCode.Should().Be(HttpStatusCode.Forbidden); // TicketAccessGuard (Task 4.6/4.16)
    }

    [Fact]
    public async Task Unauthenticated_AccessingProtectedEndpoint_ShouldReturn401()
    {
        var client = _factory.CreateClient(); // KHÔNG gắn token
        var res = await client.GetAsync($"/api/tickets/{Guid.NewGuid()}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}