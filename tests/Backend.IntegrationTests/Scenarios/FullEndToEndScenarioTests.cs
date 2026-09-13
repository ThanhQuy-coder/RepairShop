using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RepairShop.IntegrationTests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace RepairShop.IntegrationTests.Scenarios;

[Collection(nameof(IntegrationTestCollection))]
public class FullEndToEndScenarioTests
{
    private readonly CustomWebApplicationFactory _factory;
    public FullEndToEndScenarioTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task FullLifecycle_17Steps_FromRegisterToReview_AllSucceed()
    {
        var client = _factory.CreateClient();

        // 1. Register Customer (qua đúng API public, không seed thẳng DB — khác HappyPathScenarioTests
        //    Task 4.18 dùng TestUserSeeder; ở đây dùng API thật để mô phỏng đúng kịch bản demo)
        var registerRes = await client.PostAsJsonAsync("/api/auth/register", new
        { fullName = "Nguyen Van E2E", email = $"e2e-{Guid.NewGuid():N}@test.com", password = "Test@123456", phone = "0911111111" });
        registerRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var customerAuth = await registerRes.ReadAsAsync<JsonElement>();
        var customerToken = customerAuth.GetProperty("accessToken").GetString()!;

        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recepE2E");
        var technician = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "techE2E");
        var partId = await TestUserSeeder.SeedPartWithStockAsync(_factory.Services, quantity: 5);

        // 2. Receptionist creates Customer profile (liên kết đúng UserId của Customer vừa Register)
        client.AuthorizeAs(receptionist.Token);
        var loginCustRes = await client.PostAsJsonAsync("/api/auth/login", new { email = customerAuth.GetProperty("email").GetString(), password = "Test@123456" });
        // Lấy customer.UserId thật qua GetProfile
        client.AuthorizeAs(customerToken);
        var profileRes = await client.GetAsync("/api/auth/me");
        var customerUserId = (await profileRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        client.AuthorizeAs(receptionist.Token);
        var custRes = await client.PostAsJsonAsync("/api/customers", new
        { fullName = "Nguyen Van E2E", phone = $"09{Random.Shared.Next(10000000, 99999999)}", email = (string?)null, address = (string?)null, userId = customerUserId });
        var customerId = (await custRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        // 3. Create Device
        var devRes = await client.PostAsJsonAsync("/api/devices",
            new { customerId, deviceType = "Phone", brand = "iPhone", model = "13", serialNumber = $"IMEI-{Guid.NewGuid():N}"[..15] });
        var deviceId = (await devRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        // 4. Create Repair Ticket
        var ticketRes = await client.PostAsJsonAsync("/api/tickets",
            new { customerId, deviceId, issueDescription = "Pin tụt nhanh, máy nóng", notes = (string?)null, conditionNotes = "Không trầy xước", riskWarning = (string?)null });
        ticketRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var ticketId = (await ticketRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        // 5. Assign Technician
        var assignRes = await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/assign-technician", new { technicianId = technician.UserId, note = (string?)null });
        assignRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 6. Technician Diagnosis
        client.AuthorizeAs(technician.Token);
        (await client.PatchAsync($"/api/tickets/{ticketId}/start-diagnosis", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        var diagRes = await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/diagnosis", new
        { diagnosisResult = "Pin chai", rootCause = "Sử dụng lâu ngày", recommendedRepair = "Thay pin", requiredPartsNote = "Pin iPhone 13", technicalNote = (string?)null });
        diagRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 7. Create Quote
        client.AuthorizeAs(receptionist.Token);
        var quoteRes = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/quotes", new
        {
            description = "Báo giá thay pin",
            items = new[] { new { itemType = "Part", description = "Pin iPhone 13", quantity = 1, unitPrice = 350000m, partId } },
        });
        quoteRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var quoteId = (await quoteRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        // 8. Customer Approve Quote
        client.AuthorizeAs(customerToken);
        var approveRes = await client.PatchAsync($"/api/quotes/{quoteId}/approve", null);
        approveRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 9. Technician starts Repair (đã tự động IN_REPAIR sau Approve)
        client.AuthorizeAs(technician.Token);

        // 10. Add Used Part
        var usePartRes = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/parts", new { partId, quantity = 1 });
        usePartRes.StatusCode.Should().Be(HttpStatusCode.OK);

        await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/completion-notes", new { completionNotes = "Đã thay pin mới, kiểm tra hoạt động ổn định" });

        // 11. QA Pass
        (await client.PatchAsync($"/api/tickets/{ticketId}/start-qa", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        var qaRes = await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/qa-pass", new
        { functionalCheckNotes = "Sạc/xả bình thường", cosmeticCheckNotes = "Không trầy mới", originalIssueResolvedNotes = "Đã khắc phục" });
        qaRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 12. Invoice
        client.AuthorizeAs(receptionist.Token);
        var invoiceRes = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/invoice", new { paymentMethod = "Cash" });
        invoiceRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var invoiceId = (await invoiceRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        // 13. Mark Paid
        var payRes = await client.PatchAsJsonAsync($"/api/invoices/{invoiceId}/pay", new { paidAt = (DateTime?)null });
        payRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 14. Deliver
        var deliverRes = await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/deliver", new { deliveryNote = (string?)null });
        deliverRes.StatusCode.Should().Be(HttpStatusCode.OK);
        (await deliverRes.ReadAsAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("DELIVERED");

        // 15. Warranty Created
        var warrantyRes = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/warranty", new { warrantyMonths = 6, terms = "Bảo hành pin 6 tháng" });
        warrantyRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 16. Customer views Warranty
        client.AuthorizeAs(customerToken);
        var myWarrantyRes = await client.GetAsync("/api/warranty/my");
        var warranties = (await myWarrantyRes.ReadAsAsync<JsonElement>()).EnumerateArray().ToList();
        warranties.Should().ContainSingle(w => w.GetProperty("ticketId").GetGuid() == ticketId);

        // 17. Customer creates Review
        var reviewRes = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/review", new { rating = 5, comment = "Dịch vụ tốt, nhanh chóng!" });
        reviewRes.StatusCode.Should().Be(HttpStatusCode.Created);

        // Xác nhận Public Tracking hiển thị đúng trạng thái cuối
        var ticketCode = (await (await _factory.CreateClient().GetAsync($"/api/tickets/{ticketId}")).ReadAsAsync<JsonElement>());
        // (dùng client đã có quyền để lấy ticketCode)
        client.AuthorizeAs(receptionist.Token);
        var ticketDetail = await (await client.GetAsync($"/api/tickets/{ticketId}")).ReadAsAsync<JsonElement>();
        var code = ticketDetail.GetProperty("ticketCode").GetString()!;

        var trackClient = _factory.CreateClient(); // KHÔNG auth — public
        var trackRes = await trackClient.GetAsync($"/api/public/tickets/{code}/tracking");
        trackRes.StatusCode.Should().Be(HttpStatusCode.OK);
        (await trackRes.ReadAsAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("DELIVERED");
    }
}