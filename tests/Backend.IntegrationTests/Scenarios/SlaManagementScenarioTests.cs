using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using RepairShop.IntegrationTests.TestDoubles;

namespace RepairShop.IntegrationTests.Scenarios;

[Collection(nameof(IntegrationTestCollection))]
public sealed class SlaManagementScenarioTests
{
    private readonly CustomWebApplicationFactory _factory;

    public SlaManagementScenarioTests(CustomWebApplicationFactory factory)
        => _factory = factory;

    [Fact]
    public async Task Admin_CanCreateAndReadDeviceSpecificSlaPolicy()
    {
        var admin = await TestUserSeeder.SeedUserAsync(
            _factory.Services, "Admin", "adminSlaCreate");
        using var client = _factory.CreateClient();
        client.AuthorizeAs(admin.Token);

        var statusCode = "CHECKED_IN";
        var response = await client.PostAsJsonAsync("/api/sla/policies", new
        {
            statusCode,
            deviceType = "Tablet",
            durationMinutes = 90
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.ReadAsAsync<JsonElement>();
        created.GetProperty("statusCode").GetString().Should().Be(statusCode);
        created.GetProperty("deviceType").GetInt32().Should().Be(3);
        created.GetProperty("durationMinutes").GetInt32().Should().Be(90);
        created.GetProperty("isActive").GetBoolean().Should().BeTrue();

        var policies = await (await client.GetAsync("/api/sla/policies"))
            .ReadAsAsync<JsonElement>();
        policies.EnumerateArray().Should().Contain(policy =>
            policy.GetProperty("id").GetGuid() == created.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Admin_CannotCreateDuplicatePolicyForSameStatusAndDeviceType()
    {
        var admin = await TestUserSeeder.SeedUserAsync(
            _factory.Services, "Admin", "adminSlaDuplicate");
        using var client = _factory.CreateClient();
        client.AuthorizeAs(admin.Token);
        var payload = new
        {
            statusCode = "ASSIGNED",
            deviceType = "Tablet",
            durationMinutes = 120
        };

        (await client.PostAsJsonAsync("/api/sla/policies", payload))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await client.PostAsJsonAsync("/api/sla/policies", payload);

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Admin_CannotCreatePolicyForUnsupportedWorkflowStatus()
    {
        var admin = await TestUserSeeder.SeedUserAsync(
            _factory.Services, "Admin", "adminSlaInvalidStatus");
        using var client = _factory.CreateClient();
        client.AuthorizeAs(admin.Token);

        var response = await client.PostAsJsonAsync("/api/sla/policies", new
        {
            statusCode = "DELIVERED",
            deviceType = (string?)null,
            durationMinutes = 60
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task NonAdmin_CannotManageSlaPolicies()
    {
        var receptionist = await TestUserSeeder.SeedUserAsync(
            _factory.Services, "Receptionist", "receptionistSla");
        using var client = _factory.CreateClient();
        client.AuthorizeAs(receptionist.Token);

        var response = await client.GetAsync("/api/sla/policies");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
