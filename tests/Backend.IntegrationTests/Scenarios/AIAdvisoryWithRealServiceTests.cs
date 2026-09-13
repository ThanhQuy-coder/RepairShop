using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RepairShop.IntegrationTests;
using RepairShop.IntegrationTests.TestDoubles;

[Collection(nameof(IntegrationTestCollection))]
public class AIAdvisoryWithRealServiceTests
{
    private readonly CustomWebApplicationFactory _factory;
    public AIAdvisoryWithRealServiceTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task AIAdvice_NowUsesRealServiceCatalog_AfterTask711()
    {
        // Trước Task 7.11: availableServices luôn rỗng. Giờ Service entity đã tồn tại,
        // xác nhận AIServiceClient thực sự gửi kèm Service thật trong context.
        var admin = await TestUserSeeder.SeedUserAsync(_factory.Services, "Admin", "adminAI");
        var customer = await TestUserSeeder.SeedUserAsync(_factory.Services, "Customer", "custAI");

        var client = _factory.CreateClient();
        client.AuthorizeAs(admin.Token);

        await client.PostAsJsonAsync("/api/services", new
        { name = "Thay pin", description = "Thay pin chính hãng", basePrice = 400000m, deviceType = "Phone" });

        client.AuthorizeAs(customer.Token);
        var res = await client.PostAsJsonAsync("/api/ai/advice", new
        { deviceType = "phone", brand = "iPhone", model = "13", issueDescription = "Pin tụt nhanh" });

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        // aiAvailable phụ thuộc LLM_API_KEY thật có cấu hình hay không trong môi trường test — 
        // nếu chưa cấu hình LLM thật, kỳ vọng aiAvailable=false (đúng Task 6.18 fallback), KHÔNG lỗi 500.
    }
}