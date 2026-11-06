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

    private async Task<Guid> EnsureActiveTimeSlotAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = await db.TimeSlotConfigs.FirstOrDefaultAsync(s => s.IsActive && s.DayOfWeek == null);
        if (existing is not null) return existing.Id;

        var slot = new Domain.Modules.Appointments.TimeSlotConfig(new TimeOnly(9, 0), new TimeOnly(10, 0), 50);
        db.TimeSlotConfigs.Add(slot);
        await db.SaveChangesAsync();
        return slot.Id;
    }

    /// <summary>Chạy đủ Happy Path tới DELIVERED + tạo Warranty — dùng chung cho các test case dưới.</summary>
    private async Task<(HttpClient Client, Guid TicketId, SeededUser Receptionist, SeededUser Customer)> SetupDeliveredTicketWithWarranty(int warrantyMonths = 6)
    {
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recep");
        var technician = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "tech");
        var customer = await TestUserSeeder.SeedUserAsync(_factory.Services, "Customer", "cust");
        var partId = await TestUserSeeder.SeedPartWithStockAsync(_factory.Services, quantity: 10);

        var client = _factory.CreateClient();
        client.AuthorizeAs(receptionist.Token);

        var customerId = customer.CustomerId!.Value;

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
            items = new[] { new { itemType = "Part", quantity = 1, partId = (Guid?)partId, serviceId = (Guid?)null } },
        });
        var quoteId = (await quoteRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        client.AuthorizeAs(customer.Token);
        await client.PatchAsync($"/api/quotes/{quoteId}/approve", null);

        client.AuthorizeAs(technician.Token);
        await client.PostAsJsonAsync($"/api/tickets/{ticketId}/parts", new { partId, quantity = 1 });
        await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/completion-notes", new { completionNotes = "Đã thay pin" });
        await client.PatchAsync($"/api/tickets/{ticketId}/start-qa", null);
        await WorkflowHelpers.CompleteInspectionsForQaAsync(client, ticketId);
        await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/qa-pass", new
        { functionalCheckNotes = "OK", cosmeticCheckNotes = "OK", originalIssueResolvedNotes = "OK" });

        client.AuthorizeAs(receptionist.Token);
        var invoiceRes = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/invoice", new { paymentMethod = "Cash" });
        var invoiceId = (await invoiceRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();
        await client.PatchAsJsonAsync($"/api/invoices/{invoiceId}/pay", new { paidAt = (DateTime?)null });
        await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/deliver", new { deliveryNote = (string?)null });

        await client.PostAsJsonAsync($"/api/tickets/{ticketId}/warranty", new { warrantyPackageId = new Guid("00000000-0000-0000-0000-000000000203") });

        return (client, ticketId, receptionist, customer);
    }

    private async Task<(HttpClient Client, Guid TicketId, SeededUser Receptionist, SeededUser Customer)> SetupDeliveredTicketWithoutWarranty()
    {
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recep-no-warranty");
        var technician = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "tech-no-warranty");
        var customer = await TestUserSeeder.SeedUserAsync(_factory.Services, "Customer", "cust-no-warranty");
        var partId = await TestUserSeeder.SeedPartWithStockAsync(_factory.Services, quantity: 10);

        var client = _factory.CreateClient();
        client.AuthorizeAs(receptionist.Token);

        var customerId = customer.CustomerId!.Value;

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
            items = new[] { new { itemType = "Part", quantity = 1, partId = (Guid?)partId, serviceId = (Guid?)null } },
        });
        var quoteId = (await quoteRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        client.AuthorizeAs(customer.Token);
        await client.PatchAsync($"/api/quotes/{quoteId}/approve", null);

        client.AuthorizeAs(technician.Token);
        await client.PostAsJsonAsync($"/api/tickets/{ticketId}/parts", new { partId, quantity = 1 });
        await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/completion-notes", new { completionNotes = "Đã thay pin" });
        await client.PatchAsync($"/api/tickets/{ticketId}/start-qa", null);
        await WorkflowHelpers.CompleteInspectionsForQaAsync(client, ticketId);
        await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/qa-pass", new
        { functionalCheckNotes = "OK", cosmeticCheckNotes = "OK", originalIssueResolvedNotes = "OK" });

        client.AuthorizeAs(receptionist.Token);
        var invoiceRes = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/invoice", new { paymentMethod = "Cash" });
        var invoiceId = (await invoiceRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();
        await client.PatchAsJsonAsync($"/api/invoices/{invoiceId}/pay", new { paidAt = (DateTime?)null });
        await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/deliver", new { deliveryNote = (string?)null });

        return (client, ticketId, receptionist, customer);
    }

    private async Task AssertClaimRejectedAsync(HttpClient client, Guid ticketId, string issueReported, string expectedFragment, params HttpStatusCode[] allowedStatusCodes)
    {
        var slotId = await EnsureActiveTimeSlotAsync();
        var claimRes = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/warranty-claim", new
        {
            issueReported,
            appointmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            timeSlotId = slotId
        });

        claimRes.StatusCode.Should().BeOneOf(allowedStatusCodes);
        var body = await claimRes.ReadAsAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Contain(expectedFragment);
    }

    // ───────────────────────── Warranty exists? ─────────────────────────

    [Fact]
    public async Task TicketWithoutWarranty_ClaimReturnsError()
    {
        var (client, ticketId, receptionist, _) = await SetupDeliveredTicketWithoutWarranty();
        client.AuthorizeAs(receptionist.Token);

        await AssertClaimRejectedAsync(
            client,
            ticketId,
            "Pin lại tụt nhanh",
            "bảo hành",
            HttpStatusCode.BadRequest,
            HttpStatusCode.NotFound,
            HttpStatusCode.Conflict);
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
        await AssertClaimRejectedAsync(
            client,
            ticketId,
            "Pin lại tụt nhanh",
            "đã bị hủy",
            HttpStatusCode.BadRequest,
            HttpStatusCode.Conflict);
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
        await AssertClaimRejectedAsync(
            client,
            ticketId,
            "Pin lại tụt nhanh",
            "hết hạn",
            HttpStatusCode.BadRequest,
            HttpStatusCode.Conflict);
    }

    // ───────────────────────── Warranty not found (ticketId không tồn tại) ─────────────────────────

    [Fact]
    public async Task NonExistentTicket_ClaimReturns404()
    {
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recep3");
        var slotId = await EnsureActiveTimeSlotAsync();
        var client = _factory.CreateClient();
        client.AuthorizeAs(receptionist.Token);

        var res = await client.PostAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/warranty-claim", new
            {
                issueReported = "test",
                appointmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                timeSlotId = slotId
            });

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ───────────────────────── Claim duplicate ─────────────────────────

    [Fact]
    public async Task DuplicateOpenClaim_IsRejected()
    {
        var (client, ticketId, receptionist, _) = await SetupDeliveredTicketWithWarranty();
        var slotId = await EnsureActiveTimeSlotAsync();
        client.AuthorizeAs(receptionist.Token);

        var firstClaim = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/warranty-claim", new
        {
            issueReported = "Lần 1",
            appointmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            timeSlotId = slotId
        });
        firstClaim.StatusCode.Should().Be(HttpStatusCode.Created);

        // Claim thứ 2 khi claim thứ 1 vẫn đang mở -> phải reject
        var secondClaim = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/warranty-claim", new
        {
            issueReported = "Lần 2",
            appointmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            timeSlotId = slotId
        });

        secondClaim.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
        var body = await secondClaim.ReadAsAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Contain("đang xử lý");
    }

    // ───────────────────────── Claim successfully created (Eligible) ─────────────────────────

    [Fact]
    public async Task ActiveWarranty_ClaimCreatedSuccessfully()
    {
        var (client, ticketId, receptionist, _) = await SetupDeliveredTicketWithWarranty(warrantyMonths: 6);
        var slotId = await EnsureActiveTimeSlotAsync();
        client.AuthorizeAs(receptionist.Token);

        var claimRes = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/warranty-claim", new
        {
            issueReported = "Pin lại tụt nhanh như cũ",
            appointmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            timeSlotId = slotId
        });

        claimRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var claimAppointment = await claimRes.ReadAsAsync<JsonElement>();
        claimAppointment.GetProperty("status").GetString().Should().Be("Pending");
        claimAppointment.GetProperty("type").GetString().Should().Be("WarrantyRevisit");
        claimAppointment.GetProperty("parentTicketId").GetGuid().Should().Be(ticketId);
        claimAppointment.GetProperty("customerId").GetGuid().Should().NotBeEmpty();
    }
}
