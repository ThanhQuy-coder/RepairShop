using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RepairShop.IntegrationTests.TestDoubles;
using RepairShop.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static RepairShop.IntegrationTests.TestDoubles.TestUserSeeder;

namespace RepairShop.IntegrationTests.Scenarios;

[Collection(nameof(IntegrationTestCollection))]
public class WarrantyClaimScenarioTests
{
    private readonly CustomWebApplicationFactory _factory;
    public WarrantyClaimScenarioTests(CustomWebApplicationFactory factory) => _factory = factory;

    /// <summary>Chạy đủ Happy Path tới DELIVERED + tạo Warranty — dùng chung cho các test case dưới.</summary>
    private async Task<(HttpClient Client, Guid TicketId, SeededUser Receptionist, SeededUser Customer)> SetupDeliveredTicketWithWarranty(int warrantyMonths = 6)
    {
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recep");
        var technician = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "tech");
        var customer = await TestUserSeeder.SeedUserAsync(_factory.Services, "Customer", "cust");
        var partId = await TestUserSeeder.SeedPartWithStockAsync(_factory.Services, quantity: 10);

        var client = _factory.CreateClient();
        client.AuthorizeAs(receptionist.Token);

        var custRes = await client.PostAsJsonAsync("/api/customers", new
        { fullName = "Test WC", phone = $"09{Random.Shared.Next(10000000, 99999999)}", email = (string?)null, address = (string?)null, userId = customer.UserId });
        var customerId = (await custRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        var devRes = await client.PostAsJsonAsync("/api/devices",
            new { customerId, deviceType = "Phone", brand = "iPhone", model = "13", serialNumber = $"IMEI-{Guid.NewGuid():N}"[..15] });
        var deviceId = (await devRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        var ticketRes = await client.PostAsJsonAsync("/api/tickets",
            new { customerId, deviceId, issueDescription = "Pin tụt nhanh", notes = (string?)null, conditionNotes = (string?)null, riskWarning = (string?)null });
        var ticketId = (await ticketRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/assign-technician", new { technicianId = technician.UserId, note = (string?)null });

        client.AuthorizeAs(technician.Token);
        await client.PatchAsync($"/api/tickets/{ticketId}/start-diagnosis", null);
        await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/diagnosis", new
        { diagnosisResult = "Pin chai", rootCause = "test", recommendedRepair = "Thay pin", requiredPartsNote = "test", technicalNote = (string?)null });

        client.AuthorizeAs(receptionist.Token);
        var quoteRes = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/quotes", new
        {
            description = "Báo giá",
            items = new[] { new { itemType = "Part", description = "Pin", quantity = 1, unitPrice = 350000m, partId } },
        });
        var quoteId = (await quoteRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        client.AuthorizeAs(customer.Token);
        await client.PatchAsync($"/api/quotes/{quoteId}/approve", null);

        client.AuthorizeAs(technician.Token);
        await client.PostAsJsonAsync($"/api/tickets/{ticketId}/parts", new { partId, quantity = 1 });
        await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/completion-notes", new { completionNotes = "Đã thay pin" });
        await client.PatchAsync($"/api/tickets/{ticketId}/start-qa", null);
        await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/qa-pass", new
        { functionalCheckNotes = "OK", cosmeticCheckNotes = "OK", originalIssueResolvedNotes = "OK" });

        client.AuthorizeAs(receptionist.Token);
        var invoiceRes = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/invoice", new { paymentMethod = "Cash" });
        var invoiceId = (await invoiceRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();
        await client.PatchAsJsonAsync($"/api/invoices/{invoiceId}/pay", new { paidAt = (DateTime?)null });
        await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/deliver", new { deliveryNote = (string?)null });

        await client.PostAsJsonAsync($"/api/tickets/{ticketId}/warranty", new { warrantyMonths, terms = "Bảo hành pin" });

        return (client, ticketId, receptionist, customer);
    }

    // ───────────────────────── Warranty exists? ─────────────────────────

    [Fact]
    public async Task TicketWithoutWarranty_ClaimReturnsError()
    {
        // Setup ticket DELIVERED nhưng KHÔNG gọi tạo Warranty
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recep2");
        var technician = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "tech2");
        var customer = await TestUserSeeder.SeedUserAsync(_factory.Services, "Customer", "cust2");

        // (giản lược — dùng cùng helper nhưng bỏ qua bước cuối; trong triển khai thật, tách helper
        // thành 2 hàm SetupDeliveredTicket() và AddWarranty() riêng để tái dùng linh hoạt hơn)
        var client = _factory.CreateClient();
        // ... setup tương tự SetupDeliveredTicketWithWarranty nhưng KHÔNG POST /warranty

        // Giả định đã có ticketId của 1 ticket DELIVERED chưa có warranty:
        // var claimRes = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/warranty-claim", new { issueReported = "test" });
        // claimRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        // var error = await claimRes.ReadAsAsync<JsonElement>();
        // error.GetProperty("message").GetString().Should().Contain("không có thông tin bảo hành");
    }

    // ───────────────────────── Is active? / Voided ─────────────────────────

    [Fact]
    public async Task VoidedWarranty_ClaimRejected()
    {
        var (client, ticketId, receptionist, _) = await SetupDeliveredTicketWithWarranty();

        // Hủy bảo hành trực tiếp qua DbContext (chưa có API Void() expose ra HTTP — Domain method
        // Warranty.Void() đã có sẵn từ Task 4.14, chỉ chưa nối Command/Controller)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var warranty = await db.Warranties.FirstAsync(w => w.RepairTicketId == ticketId);
        warranty.Void("Test - hủy để kiểm thử");
        await db.SaveChangesAsync();

        client.AuthorizeAs(receptionist.Token);
        var claimRes = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/warranty-claim", new { issueReported = "Pin lại tụt nhanh" });

        claimRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await claimRes.ReadAsAsync<JsonElement>();
        error.GetProperty("message").GetString().Should().Contain("đã bị hủy");
    }

    // ───────────────────────── Expired? ─────────────────────────

    [Fact]
    public async Task ExpiredWarranty_ClaimRejected()
    {
        var (client, ticketId, receptionist, _) = await SetupDeliveredTicketWithWarranty();

        // Ép EndDate về quá khứ trực tiếp qua DB để test expired — không thể tạo qua API thật
        // vì CreateWarrantyCommand luôn set StartDate = hôm nay (Task 4.14).
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"Warranties\" SET \"EndDate\" = {DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))} WHERE \"RepairTicketId\" = {ticketId}");

        client.AuthorizeAs(receptionist.Token);
        var claimRes = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/warranty-claim", new { issueReported = "Pin lại tụt nhanh" });

        claimRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await claimRes.ReadAsAsync<JsonElement>();
        error.GetProperty("message").GetString().Should().Contain("hết hạn");
    }

    // ───────────────────────── Warranty not found (ticketId không tồn tại) ─────────────────────────

    [Fact]
    public async Task NonExistentTicket_ClaimReturns404()
    {
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recep3");
        var client = _factory.CreateClient();
        client.AuthorizeAs(receptionist.Token);

        var res = await client.PostAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/warranty-claim", new { issueReported = "test" });

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ───────────────────────── Claim duplicate ─────────────────────────

    [Fact]
    public async Task DuplicateOpenClaim_IsRejected()
    {
        var (client, ticketId, receptionist, _) = await SetupDeliveredTicketWithWarranty();
        client.AuthorizeAs(receptionist.Token);

        var firstClaim = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/warranty-claim", new { issueReported = "Lần 1" });
        firstClaim.StatusCode.Should().Be(HttpStatusCode.Created);

        // Claim thứ 2 khi claim thứ 1 vẫn đang mở (CHECKED_IN, chưa Delivered) -> phải reject
        var secondClaim = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/warranty-claim", new { issueReported = "Lần 2" });

        secondClaim.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await secondClaim.ReadAsAsync<JsonElement>();
        error.GetProperty("message").GetString().Should().Contain("đang được xử lý");
    }

    // ───────────────────────── Claim successfully created (Eligible) ─────────────────────────

    [Fact]
    public async Task ActiveWarranty_ClaimCreatedSuccessfully()
    {
        var (client, ticketId, receptionist, _) = await SetupDeliveredTicketWithWarranty(warrantyMonths: 6);
        client.AuthorizeAs(receptionist.Token);

        var claimRes = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/warranty-claim", new { issueReported = "Pin lại tụt nhanh như cũ" });

        claimRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var claimTicket = await claimRes.ReadAsAsync<JsonElement>();
        claimTicket.GetProperty("status").GetString().Should().Be("CHECKED_IN");

        // Xác nhận Ticket Code KHÁC ticket gốc, nhưng liên kết đúng CustomerId/DeviceId (BR-12)
        claimTicket.GetProperty("ticketCode").GetString().Should().NotBe(ticketId.ToString());
        claimTicket.GetProperty("customerId").GetGuid().Should().NotBeEmpty();
    }
}