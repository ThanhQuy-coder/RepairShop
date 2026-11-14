using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.AIAdvisory.DTOs;
using RepairShop.Application.Modules.Warranty.Queries;
using RepairShop.Domain.Common;
using RepairShop.Domain.Modules.Customers;
using RepairShop.Domain.Modules.Tickets;
using RepairShop.Domain.Modules.Warranty;
using RepairShop.Infrastructure.AI;
using Xunit;

namespace RepairShop.UnitTests.Infrastructure.AI;

public class AISecurityAndResilienceTests
{
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public StubHttpMessageHandler(HttpResponseMessage response) => _response = response;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_response);
    }

    private static readonly DateOnly Today = new(2026, 11, 9);

    private static AIConsultationClient CreateClient(
        HttpResponseMessage response,
        IWarrantyPackageRepository? packageRepo = null,
        IWarrantyRepository? warrantyRepo = null,
        IRepairTicketRepository? ticketRepo = null,
        ICustomerRepository? customerRepo = null)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(response))
        {
            BaseAddress = new Uri("http://localhost:8000")
        };

        var catalog = new Mock<IServiceCatalogQueryService>();
        catalog.Setup(c => c.GetAvailableServicesAsync(It.IsAny<string>())).ReturnsAsync([]);
        catalog.Setup(c => c.GetAvailablePartsAsync(It.IsAny<string?>())).ReturnsAsync([]);

        var slotRepo = new Mock<ITimeSlotConfigRepository>();
        slotRepo.Setup(r => r.ListAsync()).ReturnsAsync([]);

        var apptRepo = new Mock<IAppointmentRepository>();
        apptRepo.Setup(r => r.CountBookedBatchAsync(
                It.IsAny<IReadOnlyCollection<DateOnly>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var clock = new Mock<IBusinessClock>();
        clock.SetupGet(c => c.Now)
            .Returns(new DateTimeOffset(2026, 11, 9, 8, 0, 0, TimeSpan.FromHours(7)));

        return new AIConsultationClient(
            httpClient,
            catalog.Object,
            slotRepo.Object,
            apptRepo.Object,
            clock.Object,
            NullLogger<AIConsultationClient>.Instance,
            null,
            packageRepo,
            warrantyRepo,
            ticketRepo,
            customerRepo);
    }

    [Fact]
    public async Task GetConsultationAsync_WarrantyAndTracking_StripsHallucinatedIdsAndUsesContextValues()
    {
        var pkg = new WarrantyPackage("Gói chuẩn 6T", "Mô tả chuẩn", 6, "Bảo hành lỗi kỹ thuật 6 tháng");
        var pkgRepo = new Mock<IWarrantyPackageRepository>();
        pkgRepo.Setup(r => r.SearchAsync(true)).ReturnsAsync([pkg]);

        var realWarranty = new Warranty(
            "BH-20261109-001",
            Guid.NewGuid(),
            new DateOnly(2026, 9, 1),
            new DateOnly(2027, 3, 1),
            "Không áp dụng rơi vỡ",
            pkg.Id);

        var warrantyRepo = new Mock<IWarrantyRepository>();
        warrantyRepo.Setup(r => r.GetByWarrantyCodeForPublicAsync("BH-20261109-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublicWarrantyLookupView(realWarranty, "RS-20261109-001", "iPhone 13", pkg.Name));

        var staffId = Guid.NewGuid();
        var checkedInStatus = new RepairStatus(1, RepairStatusCodes.CheckedIn, "Đã tiếp nhận", 1);
        var assignedStatus = new RepairStatus(2, RepairStatusCodes.Assigned, "Đã phân công", 2);
        var diagnosingStatus = new RepairStatus(3, RepairStatusCodes.Diagnosing, "Đang kiểm tra", 3);
        var ticket = new RepairTicket(
            "RS-20261109-001", Guid.NewGuid(), Guid.NewGuid(), staffId, "Pin tụt nhanh", checkedInStatus);
        ticket.AssignTechnician(staffId, assignedStatus, staffId);
        ticket.StartDiagnosis(diagnosingStatus, staffId);

        var ticketRepo = new Mock<IRepairTicketRepository>();
        ticketRepo.Setup(r => r.GetByTicketCodeForTrackingAsync("RS-20261109-001"))
            .ReturnsAsync(ticket);

        var aiBody = new
        {
            status = "SUCCESS",
            intent = "WARRANTY",
            executedAgents = new[] { "WARRANTY", "TRACKING" },
            warranty = new
            {
                policies = new[]
                {
                    new { packageId = "pkg-fake-999", name = "Bịa 10 năm", durationMonths = 120, terms = "Free all", description = "Fake" },
                    new { packageId = pkg.Id.ToString(), name = "Tên AI bịa", durationMonths = 999, terms = "Miễn phí 100%", description = "Tampered" }
                },
                warranties = new[]
                {
                    new { warrantyCode = "BH-FAKE-999", ticketCode = "RS-FAKE", deviceLabel = "Fake", status = "Active", startDate = "2020-01-01", endDate = "2030-01-01", isExpired = false },
                    new { warrantyCode = "BH-20261109-001", ticketCode = "TAMPERED", deviceLabel = "TAMPERED", status = "Active", startDate = "2000-01-01", endDate = "2099-01-01", isExpired = false }
                },
                missingInformation = Array.Empty<string>(),
                requiresVerification = false,
                explanation = "Phiếu BH-20261109-001 còn hạn."
            },
            tracking = new
            {
                tickets = new[]
                {
                    new { ticketCode = "RS-FAKE-999", deviceLabel = "Fake", status = "Delivered", statusLabel = "Fake", statusHistory = Array.Empty<object>() },
                    new { ticketCode = "RS-20261109-001", deviceLabel = "TAMPERED", status = "Delivered", statusLabel = "AI bịa đã giao", statusHistory = Array.Empty<object>() }
                },
                missingInformation = Array.Empty<string>(),
                explanation = "Đơn đang sửa."
            },
            reason = "Đã tra cứu.",
            disclaimer = "Tham khảo."
        };

        var client = CreateClient(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(aiBody) },
            pkgRepo.Object, warrantyRepo.Object, ticketRepo.Object);

        var result = await client.GetConsultationAsync(new ConsultationRequest(
            "Phone", "iPhone", "13", "Kiểm tra BH-20261109-001 và RS-20261109-001",
            Today.AddDays(1), AllowWarranty: true, AllowTracking: true,
            TicketCode: "RS-20261109-001", WarrantyCode: "BH-20261109-001"));

        Assert.True(result.AiAvailable);
        Assert.Equal("SUCCESS", result.Status);

        Assert.NotNull(result.Warranty);
        Assert.Single(result.Warranty!.Policies);
        Assert.Equal(pkg.Name, result.Warranty.Policies[0].Name);
        Assert.Equal(6, result.Warranty.Policies[0].DurationMonths);
        Assert.Single(result.Warranty.Warranties);
        Assert.Equal("BH-20261109-001", result.Warranty.Warranties[0].WarrantyCode);
        Assert.Equal("iPhone 13", result.Warranty.Warranties[0].DeviceLabel);
        Assert.Equal("2027-03-01", result.Warranty.Warranties[0].EndDate);
        Assert.True(result.Warranty.RequiresVerification);

        Assert.NotNull(result.Tracking);
        Assert.Single(result.Tracking!.Tickets);
        Assert.Equal("RS-20261109-001", result.Tracking.Tickets[0].TicketCode);
        Assert.Equal(RepairStatusCodes.Diagnosing, result.Tracking.Tickets[0].Status);
        Assert.Equal("Đang kiểm tra", result.Tracking.Tickets[0].StatusLabel);
        Assert.Equal(2, result.Tracking.Tickets[0].StatusHistory.Count);
    }

    [Fact]
    public async Task GetConsultationAsync_WhenAllWarrantyAndTrackingHallucinated_ReturnsNoMatch()
    {
        var aiBody = new
        {
            status = "SUCCESS",
            intent = "WARRANTY",
            executedAgents = new[] { "WARRANTY" },
            warranty = new
            {
                policies = Array.Empty<object>(),
                warranties = new[]
                {
                    new { warrantyCode = "BH-FAKE-000", ticketCode = "RS-FAKE", deviceLabel = "iPhone", status = "Active", startDate = "2026-01-01", endDate = "2027-01-01", isExpired = false }
                },
                missingInformation = Array.Empty<string>(),
                requiresVerification = true,
                explanation = "Dữ liệu giả."
            },
            reason = "Fake warranty",
            disclaimer = "Tham khảo."
        };

        var client = CreateClient(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(aiBody) });

        var result = await client.GetConsultationAsync(new ConsultationRequest(
            "Phone", "iPhone", "13", "Kiểm tra bảo hành BH-FAKE-000",
            AllowWarranty: true, AllowTracking: true));

        Assert.True(result.AiAvailable);
        Assert.Equal("NO_MATCH", result.Status);
        Assert.NotNull(result.Warranty);
        Assert.Empty(result.Warranty!.Warranties);
    }

    [Fact]
    public async Task GetConsultationAsync_GuestWithoutExplicitCode_NeverQueriesCustomerHistory()
    {
        var customerRepo = new Mock<ICustomerRepository>();
        var warrantyRepo = new Mock<IWarrantyRepository>();
        var ticketRepo = new Mock<IRepairTicketRepository>();
        var pkgRepo = new Mock<IWarrantyPackageRepository>();
        pkgRepo.Setup(r => r.SearchAsync(true)).ReturnsAsync([]);

        var aiBody = new
        {
            status = "NEED_CLARIFICATION",
            intent = "TRACKING",
            executedAgents = new[] { "TRACKING" },
            tracking = new
            {
                tickets = Array.Empty<object>(),
                missingInformation = new[] { "ticketCode" },
                explanation = "Vui lòng nhập mã phiếu."
            },
            missingInformation = new[] { "ticketCode" },
            reason = "Vui lòng nhập mã phiếu.",
            disclaimer = "Tham khảo."
        };

        var client = CreateClient(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(aiBody) },
            pkgRepo.Object, warrantyRepo.Object, ticketRepo.Object, customerRepo.Object);

        // Ngay cả khi UserId hoặc Phone xuất hiện nhưng ActorType = GUEST, không bao giờ truy vấn DB cá nhân.
        var result = await client.GetConsultationAsync(new ConsultationRequest(
            "Phone", "iPhone", "13", "Máy tôi sửa xong chưa",
            ActorType: "GUEST", Phone: "0901234567", UserId: Guid.NewGuid(),
            AllowWarranty: true, AllowTracking: true));

        Assert.Equal("NEED_CLARIFICATION", result.Status);
        customerRepo.Verify(r => r.GetByUserIdAsync(It.IsAny<Guid>()), Times.Never);
        warrantyRepo.Verify(r => r.GetByCustomerIdAsync(It.IsAny<Guid>()), Times.Never);
        ticketRepo.Verify(r => r.GetByCustomerForHistoryAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetConsultationAsync_CustomerWithUserId_LoadsCustomerWarrantyAndTrackingContext()
    {
        var userId = Guid.NewGuid();
        var customer = new Customer("Nguyễn Văn A", "0901234567", null, null, userId);

        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(customer);

        var w = new Warranty("BH-CUS-01", Guid.NewGuid(), new DateOnly(2026, 10, 1), new DateOnly(2027, 4, 1));
        var warrantyRepo = new Mock<IWarrantyRepository>();
        warrantyRepo.Setup(r => r.GetByCustomerIdAsync(customer.Id))
            .ReturnsAsync([new CustomerWarrantyView(w, Guid.NewGuid(), "RS-CUS-01", "Samsung Galaxy S24")]);

        var staffId = Guid.NewGuid();
        var ticket = new RepairTicket(
            "RS-CUS-01", customer.Id, Guid.NewGuid(), staffId, "Lỗi sạc",
            new RepairStatus(1, RepairStatusCodes.CheckedIn, "Đã tiếp nhận", 1));
        ticket.AssignTechnician(staffId, new RepairStatus(2, RepairStatusCodes.Assigned, "Đã phân công", 2), staffId);
        ticket.StartDiagnosis(new RepairStatus(3, RepairStatusCodes.Diagnosing, "Đang kiểm tra", 3), staffId);
        var ticketRepo = new Mock<IRepairTicketRepository>();
        ticketRepo.Setup(r => r.GetByCustomerForHistoryAsync(customer.Id))
            .ReturnsAsync([ticket]);

        var aiBody = new
        {
            status = "SUCCESS",
            intent = "TRACKING",
            executedAgents = new[] { "TRACKING" },
            tracking = new
            {
                tickets = new[]
                {
                    new { ticketCode = "RS-CUS-01", deviceLabel = "Samsung Galaxy S24", status = "DIAGNOSING", statusLabel = "Đang kiểm tra", statusHistory = Array.Empty<object>() }
                },
                missingInformation = Array.Empty<string>(),
                explanation = "Phiếu RS-CUS-01 đang kiểm tra."
            },
            reason = "Phiếu RS-CUS-01 đang kiểm tra.",
            disclaimer = "Tham khảo."
        };

        var client = CreateClient(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(aiBody) },
            null, warrantyRepo.Object, ticketRepo.Object, customerRepo.Object);

        var result = await client.GetConsultationAsync(new ConsultationRequest(
            "Phone", "Samsung", "Galaxy S24", "Tiến độ sửa máy của tôi",
            ActorType: "CUSTOMER", UserId: userId,
            AllowWarranty: true, AllowTracking: true));

        Assert.Equal("SUCCESS", result.Status);
        customerRepo.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        warrantyRepo.Verify(r => r.GetByCustomerIdAsync(customer.Id), Times.Once);
        ticketRepo.Verify(r => r.GetByCustomerForHistoryAsync(customer.Id), Times.Once);
        Assert.NotNull(result.Tracking);
        Assert.Equal("RS-CUS-01", result.Tracking!.Tickets.Single().TicketCode);
    }

    [Fact]
    public async Task TrackWarrantyByCodeQueryHandler_HandlesActiveVoidedAndNotFound()
    {
        var futureStart = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10));
        var futureEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(170));
        var active = new Warranty("BH-ACT-1", Guid.NewGuid(), futureStart, futureEnd, "6 tháng");

        var voided = new Warranty("BH-VOID-1", Guid.NewGuid(), futureStart, futureEnd, "6 tháng");
        voided.Void("Vào nước");

        var repo = new Mock<IWarrantyRepository>();
        repo.Setup(r => r.GetByWarrantyCodeForPublicAsync("BH-ACT-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublicWarrantyLookupView(active, "RS-1", "iPhone 13", "Tiêu chuẩn"));
        repo.Setup(r => r.GetByWarrantyCodeForPublicAsync("BH-VOID-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublicWarrantyLookupView(voided, "RS-2", "Dell XPS", null));
        repo.Setup(r => r.GetByWarrantyCodeForPublicAsync("BH-NONE", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PublicWarrantyLookupView?)null);

        var handler = new TrackWarrantyByCodeQueryHandler(repo.Object);

        var resActive = await handler.Handle(new TrackWarrantyByCodeQuery("BH-ACT-1"), CancellationToken.None);
        Assert.Equal("Active", resActive.Status);
        Assert.False(resActive.IsExpired);
        Assert.Equal("Tiêu chuẩn", resActive.PackageName);

        var resVoided = await handler.Handle(new TrackWarrantyByCodeQuery("BH-VOID-1"), CancellationToken.None);
        Assert.Equal("Voided", resVoided.Status);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new TrackWarrantyByCodeQuery("BH-NONE"), CancellationToken.None));
    }
}
