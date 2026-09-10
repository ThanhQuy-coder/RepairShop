namespace RepairShop.IntegrationTests.Scenarios;

[Collection(nameof(IntegrationTestCollection))]
public class WarrantyClaimScenarioTests
{
    private readonly CustomWebApplicationFactory _factory;
    public WarrantyClaimScenarioTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task WarrantyExpired_ClaimRejected()
    {
        // Arrange: tạo Warranty với EndDate trong QUÁ KHỨ để test expired
        // (yêu cầu tạo Warranty trực tiếp qua DbContext trong test, vì API CreateWarranty
        // luôn set StartDate = hôm nay — không thể tạo "đã hết hạn" qua đúng luồng API thật)
        // ... (setup ticket tới DELIVERED như HappyPathScenarioTests, sau đó seed Warranty hết hạn qua DbContext)
    }

    [Fact]
    public async Task WarrantyActive_ClaimCreatedSuccessfully()
    {
        // Tương tự Happy Path đến DELIVERED + CreateWarranty(warrantyMonths: 6) -> claim thành công
    }

    [Fact]
    public async Task TicketWithoutWarranty_ClaimReturnsError()
    {
        // Ticket DELIVERED nhưng CHƯA tạo Warranty -> claim phải fail với message rõ ràng
    }

    [Fact]
    public async Task DuplicateOpenClaim_IsRejected()
    {
        // Tạo claim lần 1 (đang CHECKED_IN, chưa Delivered) -> tạo claim lần 2 trên CÙNG ticket gốc -> fail
    }
}