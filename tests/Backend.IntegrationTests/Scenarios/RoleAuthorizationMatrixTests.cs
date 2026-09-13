using System.Net;
using System.Net.Http.Json;
using RepairShop.IntegrationTests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace RepairShop.IntegrationTests.Scenarios;

[Collection(nameof(IntegrationTestCollection))]
public class RoleAuthorizationMatrixTests
{
    private readonly CustomWebApplicationFactory _factory;
    public RoleAuthorizationMatrixTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Theory]
    [InlineData("Receptionist", HttpStatusCode.Forbidden)]
    [InlineData("Technician", HttpStatusCode.Forbidden)]
    [InlineData("Customer", HttpStatusCode.Forbidden)]
    public async Task NonAdmin_CannotAccessUserManagement(string role, HttpStatusCode expected)
    {
        var user = await TestUserSeeder.SeedUserAsync(_factory.Services, role, $"u-{role}");
        var client = _factory.CreateClient();
        client.AuthorizeAs(user.Token);

        var res = await client.GetAsync("/api/users");
        res.StatusCode.Should().Be(expected);
    }

    [Theory]
    [InlineData("Technician")]
    [InlineData("Customer")]
    public async Task NonReceptionistAdmin_CannotCreateTicket(string role)
    {
        var user = await TestUserSeeder.SeedUserAsync(_factory.Services, role, $"u-{role}2");
        var client = _factory.CreateClient();
        client.AuthorizeAs(user.Token);

        var res = await client.PostAsJsonAsync("/api/tickets", new
        { customerId = Guid.NewGuid(), deviceId = Guid.NewGuid(), issueDescription = "test", notes = (string?)null, conditionNotes = (string?)null, riskWarning = (string?)null });

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Customer_CannotCreateQuote()
    {
        var customer = await TestUserSeeder.SeedUserAsync(_factory.Services, "Customer", "custQuote");
        var client = _factory.CreateClient();
        client.AuthorizeAs(customer.Token);

        var res = await client.PostAsJsonAsync($"/api/tickets/{Guid.NewGuid()}/quotes",
            new { description = "test", items = Array.Empty<object>() });

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Technician_CannotApproveQuote()
    {
        var technician = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "techQuote");
        var client = _factory.CreateClient();
        client.AuthorizeAs(technician.Token);

        var res = await client.PatchAsync($"/api/quotes/{Guid.NewGuid()}/approve", null);
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Receptionist_CannotAccessReports()
    {
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recepReport");
        var client = _factory.CreateClient();
        client.AuthorizeAs(receptionist.Token);

        var res = await client.GetAsync("/api/reports/dashboard-summary");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Unauthenticated_CannotAccessAnyProtectedEndpoint()
    {
        var client = _factory.CreateClient(); // không gắn token

        var endpoints = new[] { "/api/customers", "/api/tickets", "/api/parts", "/api/reports/dashboard-summary" };
        foreach (var endpoint in endpoints)
        {
            var res = await client.GetAsync(endpoint);
            res.StatusCode.Should().Be(HttpStatusCode.Unauthorized, because: $"{endpoint} phải yêu cầu đăng nhập");
        }
    }
}