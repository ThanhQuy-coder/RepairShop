using System.Net;
using System.Net.Http.Json;
using RepairShop.IntegrationTests.TestDoubles;
using FluentAssertions;
using static RepairShop.IntegrationTests.TestDoubles.HttpClientExtensions;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RepairShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace RepairShop.IntegrationTests.Scenarios;

[Collection(nameof(IntegrationTestCollection))]
public class HappyPathScenarioTests
{
    private readonly CustomWebApplicationFactory _factory;

    public HappyPathScenarioTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task FullWorkflow_FromCustomerCreation_ToWarranty_ShouldSucceedAtEveryStep()
    {
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recep");
        var technician = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "tech");
        var customer = await TestUserSeeder.SeedUserAsync(_factory.Services, "Customer", "cust");
        var partId = await TestUserSeeder.SeedPartWithStockAsync(_factory.Services, quantity: 10);

        var client = _factory.CreateClient();

        // 1. Create Customer (Receptionist tạo hồ sơ walk-in khách hàng)
        client.AuthorizeAs(receptionist.Token);
        // var createCustomerRes = await client.PostAsJsonAsync("/api/customers", new
        // {
        //     fullName = "Nguyen Van A",
        //     phone = $"09{Random.Shared.Next(10000000, 99999999)}",
        //     email = "customer@gmail.com",
        //     address = "TP.HCM",
        //     userId = customer.UserId
        // });
        // createCustomerRes.StatusCode.Should().Be(HttpStatusCode.Created);
        // var customerJson = await createCustomerRes.ReadAsAsync<JsonElement>();
        // var customerId = customerJson.GetProperty("id").GetGuid();

        // Khởi tạo scope để đọc Database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Tìm Customer theo UserId
        var customerEntity = await db.Customers.FirstAsync(c => c.UserId == customer.UserId);
        Guid customerId = customerEntity.Id;

        // 2. Create Device
        var createDeviceRes = await client.PostAsJsonAsync("/api/devices",
            new { customerId, deviceType = "Phone", brand = "iPhone", model = "13", serialNumber = $"IMEI-{Guid.NewGuid():N}"[..15] });
        createDeviceRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var deviceId = (await createDeviceRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        // 3. Create Ticket
        var createTicketRes = await client.PostAsJsonAsync("/api/tickets",
            new
            {
                customerId,
                deviceId,
                issueDescription = "Pin tụt nhanh, máy nóng",
                notes = (string?)null,
                conditionNotes = "Trầy nhẹ góc trên",
                riskWarning = (string?)null
            });
        createTicketRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var ticket = await createTicketRes.ReadAsAsync<JsonElement>();
        var ticketId = ticket.GetProperty("id").GetGuid();
        ticket.GetProperty("status").GetString().Should().Be("CHECKED_IN");

        // 4. Upload Image (dùng FakeFileStorageService, không gọi Cloudinary thật)
        using var multipart = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([1, 2, 3]);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        multipart.Add(fileContent, "file", "before.jpg");
        multipart.Add(new StringContent("BeforeRepair"), "imageType");
        var uploadRes = await client.PostAsync($"/api/tickets/{ticketId}/images", multipart);
        uploadRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Assign Technician — CHECKED_IN -> ASSIGNED
        var assignRes = await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/assign-technician",
            new { technicianId = technician.UserId, note = "Giao cho kỹ thuật viên A" });
        assignRes.StatusCode.Should().Be(HttpStatusCode.OK);
        (await assignRes.ReadAsAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("ASSIGNED");

        // 6. Diagnosis — ASSIGNED -> DIAGNOSING -> ghi kết quả
        client.AuthorizeAs(technician.Token);
        var startDiagRes = await client.PatchAsync($"/api/tickets/{ticketId}/start-diagnosis", null);
        startDiagRes.StatusCode.Should().Be(HttpStatusCode.OK);
        (await startDiagRes.ReadAsAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("DIAGNOSING");

        var submitDiagRes = await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/diagnosis", new
        {
            diagnosisResult = "Pin chai, dung lượng còn 62%",
            rootCause = "Pin sử dụng lâu ngày",
            recommendedRepair = "Thay pin chính hãng",
            requiredPartsNote = "Pin iPhone 13",
            technicalNote = "Đã kiểm tra bo mạch, ổn định"
        });
        submitDiagRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 7. Create Quote — DIAGNOSING -> WAITING_APPROVAL
        client.AuthorizeAs(receptionist.Token);
        var createQuoteRes = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/quotes", new
        {
            description = "Báo giá thay pin",
            items = new object[]
            {
                new { itemType = "Service", description = "Công thay pin", quantity = 1, unitPrice = 100000m, partId = (Guid?)null },
                new { itemType = "Part", description = "Pin iPhone 13", quantity = 1, unitPrice = 350000m, partId }
            }
        });
        createQuoteRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var quote = await createQuoteRes.ReadAsAsync<JsonElement>();
        var quoteId = quote.GetProperty("id").GetGuid();
        quote.GetProperty("totalAmount").GetDecimal().Should().Be(450000m);

        // 8. Approve Quote — WAITING_APPROVAL -> IN_REPAIR
        client.AuthorizeAs(customer.Token);
        var approveRes = await client.PatchAsync($"/api/quotes/{quoteId}/approve", null);
        approveRes.StatusCode.Should().Be(HttpStatusCode.OK);
        (await approveRes.ReadAsAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("Approved");

        // 9. Repair — ghi Parts Used, Repair Notes, Completion Notes
        client.AuthorizeAs(technician.Token);
        var usePartRes = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/parts", new { partId, quantity = 1 });
        usePartRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var repairNoteRes = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/repair-notes", new { note = "Đã thay pin mới" });
        repairNoteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var afterMultipart = new MultipartFormDataContent();
        var afterFile = new ByteArrayContent([4, 5, 6]);
        afterFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        afterMultipart.Add(afterFile, "file", "after.jpg");
        afterMultipart.Add(new StringContent("AfterRepair"), "imageType");
        var uploadAfterRes = await client.PostAsync($"/api/tickets/{ticketId}/images", afterMultipart);
        uploadAfterRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var completionRes = await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/completion-notes",
            new { completionNotes = "Đã thay pin, test sạc/xả ổn định" });
        completionRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 10. QA Pass — IN_REPAIR -> QA_TESTING -> READY_FOR_PICKUP
        var startQaRes = await client.PatchAsync($"/api/tickets/{ticketId}/start-qa", null);
        startQaRes.StatusCode.Should().Be(HttpStatusCode.OK);
        (await startQaRes.ReadAsAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("QA_TESTING");

        var qaPassRes = await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/qa-pass", new
        {
            functionalCheckNotes = "Sạc/xả pin bình thường, không nóng máy",
            cosmeticCheckNotes = "Không có vết trầy mới phát sinh",
            originalIssueResolvedNotes = "Đã khắc phục hoàn toàn lỗi pin tụt nhanh"
        });
        qaPassRes.StatusCode.Should().Be(HttpStatusCode.OK);
        (await qaPassRes.ReadAsAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("READY_FOR_PICKUP");

        // 11. Delivery — cần Invoice + Paid trước khi Deliver được (Task 4.12 enforce)
        client.AuthorizeAs(receptionist.Token);
        var invoiceRes = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/invoice", new { paymentMethod = "Cash" });
        invoiceRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var invoiceId = (await invoiceRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        var payRes = await client.PatchAsJsonAsync($"/api/invoices/{invoiceId}/pay", new { paidAt = (DateTime?)null });
        payRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var deliverRes = await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/deliver", new { deliveryNote = "Khách đã kiểm tra máy tại quầy" });
        deliverRes.StatusCode.Should().Be(HttpStatusCode.OK);
        (await deliverRes.ReadAsAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("DELIVERED");

        // 12. Warranty
        var warrantyRes = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/warranty", new { warrantyMonths = 6, terms = "Bảo hành lỗi pin/linh kiện đã thay" });
        warrantyRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var warranty = await warrantyRes.ReadAsAsync<JsonElement>();
        warranty.GetProperty("warrantyCode").GetString().Should().StartWith("WR-");
        warranty.GetProperty("status").GetString().Should().Be("Active");

        // Acceptance: Status History phải phản ánh ĐỦ hành trình (Task 4.13)
        var historyRes = await client.GetAsync($"/api/tickets/{ticketId}/status-history");
        var history = (await historyRes.ReadAsAsync<JsonElement>()).EnumerateArray().ToList();
        var expectedSequence = new[] { "CHECKED_IN", "ASSIGNED", "DIAGNOSING", "WAITING_APPROVAL", "IN_REPAIR", "QA_TESTING", "READY_FOR_PICKUP", "DELIVERED" };
        history.Select(h => h.GetProperty("toStatus").GetString()).Should().Equal(expectedSequence);
    }
}