using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using RepairShop.IntegrationTests.TestDoubles;
using Xunit;

namespace RepairShop.IntegrationTests.Scenarios;

[Collection(nameof(IntegrationTestCollection))]
public class ExtendedModulesScenarioTests
{
    private readonly CustomWebApplicationFactory _factory;

    public ExtendedModulesScenarioTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Auth_WrongPasswordReturns401_AndDuplicateEmailReturns409()
    {
        var client = _factory.CreateClient();
        var email = $"auth-{Guid.NewGuid():N}@test.com";

        var regRes = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Auth Tester",
            email,
            password = "Password@123",
            phone = $"09{Random.Shared.Next(10000000, 99999999)}"
        });
        regRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Đăng ký trùng email -> 409 Conflict
        var dupRes = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Auth Tester 2",
            email,
            password = "Password@123",
            phone = $"09{Random.Shared.Next(10000000, 99999999)}"
        });
        dupRes.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Đăng nhập sai mật khẩu -> 401 Unauthorized
        var wrongLoginRes = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "WrongPassword@999"
        });
        wrongLoginRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PublicTracking_InvalidFormatReturns400_AndNonExistentCodeReturns404()
    {
        var client = _factory.CreateClient();

        // Sai định dạng mã phiếu -> 400 BadRequest (FluentValidation)
        var invalidRes = await client.GetAsync("/api/public/tickets/INVALID-CODE/tracking");
        invalidRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Đúng định dạng RT-yyyyMMdd-xxxx nhưng không tồn tại -> 404 NotFound
        var notFoundRes = await client.GetAsync("/api/public/tickets/RT-20991231-9999/tracking");
        notFoundRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Inspections_QaPassWithoutChecklistReturns400_AndFailWithoutNoteReturns400()
    {
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recepInsp");
        var technician = await TestUserSeeder.SeedUserAsync(_factory.Services, "Technician", "techInsp");
        var customer = await TestUserSeeder.SeedUserAsync(_factory.Services, "Customer", "custInsp");

        var client = _factory.CreateClient();
        client.AuthorizeAs(receptionist.Token);
        var (ticketId, quoteId) = await WorkflowHelpers.CreateTicketUpToQuote(
            client, technician.Token, technician.UserId, customer.UserId, customer.CustomerId);

        client.AuthorizeAs(customer.Token);
        await client.PatchAsync($"/api/quotes/{quoteId}/approve", null);

        client.AuthorizeAs(technician.Token);
        await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/completion-notes",
            new { completionNotes = "Đã sửa chữa xong" });
        (await client.PatchAsync($"/api/tickets/{ticketId}/start-qa", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // Gọi qa-pass khi chưa điền 3 checklist -> 400 BadRequest
        var prematureQaPass = await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/qa-pass", new
        {
            functionalCheckNotes = "OK",
            cosmeticCheckNotes = "OK",
            originalIssueResolvedNotes = "OK"
        });
        prematureQaPass.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Lưu checklist RepairVerification với trạng thái NotFixed nhưng thiếu Note -> 400 BadRequest
        var invalidChecklist = await client.PutAsJsonAsync($"/api/tickets/{ticketId}/inspections/RepairVerification", new
        {
            items = new[]
            {
                new
                {
                    templateId = (Guid?)null,
                    itemName = "Kiểm tra màn hình",
                    status = "NotFixed",
                    note = (string?)null
                }
            }
        });
        invalidChecklist.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WarrantyPackages_AdminCanCrud_AndNonAdminIsForbidden()
    {
        var admin = await TestUserSeeder.SeedUserAsync(_factory.Services, "Admin", "adminWp");
        var receptionist = await TestUserSeeder.SeedUserAsync(_factory.Services, "Receptionist", "recepWp");

        using var client = _factory.CreateClient();

        // Receptionist không được tạo gói bảo hành -> 403 Forbidden
        client.AuthorizeAs(receptionist.Token);
        var forbiddenCreate = await client.PostAsJsonAsync("/api/warranty-packages", new
        {
            name = "Gói 12 tháng",
            description = "Bảo hành mở rộng",
            durationMonths = 12,
            terms = "Áp dụng lỗi nhà sản xuất"
        });
        forbiddenCreate.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Admin tạo, cập nhật, bật/tắt, xóa gói bảo hành chưa sử dụng -> thành công
        client.AuthorizeAs(admin.Token);
        var createRes = await client.PostAsJsonAsync("/api/warranty-packages", new
        {
            name = $"Gói VIP {Guid.NewGuid():N}"[..16],
            description = "Bảo hành VIP 24 tháng",
            durationMonths = 24,
            terms = "Bảo hành toàn diện linh kiện thay thế"
        });
        createRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await createRes.ReadAsAsync<JsonElement>();
        var pkgId = created.GetProperty("id").GetGuid();

        var updateRes = await client.PutAsJsonAsync($"/api/warranty-packages/{pkgId}", new
        {
            name = "Gói VIP Updated",
            description = "Mô tả cập nhật",
            durationMonths = 18,
            terms = "Điều khoản cập nhật"
        });
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);
        (await updateRes.ReadAsAsync<JsonElement>()).GetProperty("durationMonths").GetInt32().Should().Be(18);

        var toggleRes = await client.PatchAsJsonAsync($"/api/warranty-packages/{pkgId}/active", new { isActive = false });
        toggleRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var deleteRes = await client.DeleteAsync($"/api/warranty-packages/{pkgId}");
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
