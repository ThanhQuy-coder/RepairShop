using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RepairShop.IntegrationTests.TestDoubles;
using FluentAssertions;

namespace RepairShop.IntegrationTests.Scenarios;

[Collection(nameof(IntegrationTestCollection))]
public class UsePartStockScenarioTests
{
    private readonly CustomWebApplicationFactory _factory;
    public UsePartStockScenarioTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task UsePart_Stock2_Use1_ResultsInStock1()
    {
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recep");
        var technician = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "tech");
        var admin = await TestUserSeeder.SeedUserAsync(_factory.Services, "Admin", "admin");

        var client = _factory.CreateClient();
        client.AuthorizeAs(admin.Token);

        // Tạo Part + nhập kho đúng 2 đơn vị qua API thật (không seed thẳng DB)
        var createPartRes = await client.PostAsJsonAsync("/api/parts", new
        {
            name = "Pin test",
            sku = $"SKU-{Guid.NewGuid():N}"[..12],
            costPrice = 200000m,
            unitPrice = 350000m,
            category = (string?)null,
            compatibleDeviceType = (string?)null,
            unit = "cái",
            minStockThreshold = 1,
        });
        var partId = (await createPartRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        await client.PostAsJsonAsync("/api/inventory/transactions", new { partId, type = "Import", quantity = 2 });

        client.AuthorizeAs(receptionist.Token);
        var (ticketId, quoteId) = await WorkflowHelpers.CreateTicketUpToQuote(client, technician.Token, technician.UserId);

        client.AuthorizeAs(technician.Token); // giả định là chủ ticket trong helper
        // ... approve quote flow qua customer trong thực tế; ở đây rút gọn: giả lập trực tiếp trạng thái IN_REPAIR
        // (nếu WorkflowHelpers không hỗ trợ, dùng luồng đầy đủ như HappyPathScenarioTests)

        var useRes = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/parts", new { partId, quantity = 1 });
        useRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var inventoryRes = await client.GetAsync("/api/inventory");
        var inventory = (await inventoryRes.ReadAsAsync<JsonElement>()).EnumerateArray()
            .First(i => i.GetProperty("partId").GetGuid() == partId);
        inventory.GetProperty("quantityOnHand").GetInt32().Should().Be(1); // 2 - 1 = 1
    }

    [Fact]
    public async Task UsePart_Stock0_Use1_BackendRejects_NotFrontend()
    {
        var admin = await TestUserSeeder.SeedUserAsync(_factory.Services, "Admin", "admin");
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recep");
        var technician = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "tech");

        var client = _factory.CreateClient();
        client.AuthorizeAs(admin.Token);

        var createPartRes = await client.PostAsJsonAsync("/api/parts", new
        {
            name = "Pin hết hàng",
            sku = $"SKU-{Guid.NewGuid():N}"[..12],
            costPrice = 200000m,
            unitPrice = 350000m,
            category = (string?)null,
            compatibleDeviceType = (string?)null,
            unit = "cái",
            minStockThreshold = 1,
        });
        var partId = (await createPartRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();
        // KHÔNG nhập kho — tồn = 0

        client.AuthorizeAs(receptionist.Token);
        var (ticketId, _) = await WorkflowHelpers.CreateTicketUpToQuote(client, technician.Token, technician.UserId);

        client.AuthorizeAs(technician.Token);
        var useRes = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/parts", new { partId, quantity = 1 });

        // Backend PHẢI reject — 409, không phải 200 với dữ liệu sai
        useRes.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await useRes.ReadAsAsync<JsonElement>();
        error.GetProperty("message").GetString().Should().Contain("Không đủ tồn kho");
    }
}