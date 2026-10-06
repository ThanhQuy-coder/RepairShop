using System.Net.Http.Json;
using System.Text.Json;
using RepairShop.IntegrationTests.TestDoubles;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace RepairShop.IntegrationTests.Scenarios;

[Collection(nameof(IntegrationTestCollection))]
public class SignalRRealtimeScenarioTests
{
    private readonly CustomWebApplicationFactory _factory;
    public SignalRRealtimeScenarioTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task AssignTechnician_PushesNotification_ToTechnicianHubConnection()
    {
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recepRT");
        var technician = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "techRT");

        // Mô phỏng "Tab 2 — Technician" đang mở sẵn: kết nối Hub thật qua TestServer, dùng JWT của Technician
        var httpClient = _factory.CreateClient();
        var hubConnection = new HubConnectionBuilder()
            .WithUrl($"{httpClient.BaseAddress}hubs/notifications?access_token={technician.Token}",
                options => options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler())
            .Build();

        TaskCompletionSource<JsonElement> receivedTcs = new();
        hubConnection.On<JsonElement>("ReceiveNotification", payload => receivedTcs.TrySetResult(payload));

        await hubConnection.StartAsync();

        // Mô phỏng "Tab 1 — Receptionist" assign ticket
        var client = _factory.CreateClient();
        client.AuthorizeAs(receptionist.Token);

        var custRes = await client.PostAsJsonAsync("/api/customers", new
        { fullName = "Test RT", phone = "0966666666", email = (string?)null, address = (string?)null, userId = (Guid?)null });
        var customerId = (await custRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();
        var devRes = await client.PostAsJsonAsync("/api/devices",
            new { customerId, deviceType = "Phone", brand = "Test", model = "RT", serialNumber = $"SN-{Guid.NewGuid():N}"[..15] });
        var deviceId = (await devRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();
        var ticketRes = await client.PostAsJsonAsync("/api/tickets",
            new { customerId, deviceId, issueDescription = "test", notes = (string?)null, conditionNotes = (string?)null, riskWarning = (string?)null });
        var ticketId = (await ticketRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/assign-technician",
            new { technicianId = technician.UserId, note = (string?)null });

        // Technician phải nhận được push TRONG VÀI GIÂY — không phải chờ polling 30s
        var completed = await Task.WhenAny(receivedTcs.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        completed.Should().Be(receivedTcs.Task, because: "Technician phải nhận notification qua SignalR gần như ngay lập tức");

        var payload = await receivedTcs.Task;
        payload.GetProperty("type").GetString().Should().Be("TicketAssigned");

        await hubConnection.DisposeAsync();
    }

    [Fact]
    public async Task UnauthenticatedConnection_ToHub_IsRejected()
    {
        // NFR-028 regression: xác nhận lại Hub vẫn chặn anonymous dù đã thêm JWT-over-querystring
        var httpClient = _factory.CreateClient();
        var hubConnection = new HubConnectionBuilder()
            .WithUrl($"{httpClient.BaseAddress}hubs/notifications", // KHÔNG kèm access_token
                options => options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler())
            .Build();

        var act = async () => await hubConnection.StartAsync();
        await act.Should().ThrowAsync<Exception>(); // bị từ chối ở bước negotiate/handshake do thiếu auth

        await hubConnection.DisposeAsync();
    }
}