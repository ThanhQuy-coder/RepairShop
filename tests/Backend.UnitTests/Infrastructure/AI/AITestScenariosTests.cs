using System.Net;
using System.Net.Http.Json;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.AIAdvisory.DTOs;
using RepairShop.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace RepairShop.UnitTests.Infrastructure.AI;

public class AITestScenariosTests
{
    private static AIServiceClient BuildClient(
        HttpMessageHandler handler,
        List<CatalogServiceItem>? services = null,
        List<CatalogPartItem>? parts = null,
        TimeSpan? timeout = null,
        IAICircuitBreaker? breaker = null)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8000"),
            Timeout = timeout ?? TimeSpan.FromSeconds(5),
        };

        var catalogMock = new Mock<IServiceCatalogQueryService>();
        catalogMock.Setup(c => c.GetAvailableServicesAsync(It.IsAny<string>())).ReturnsAsync(services ?? new List<CatalogServiceItem>());
        catalogMock.Setup(c => c.GetAvailablePartsAsync()).ReturnsAsync(parts ?? new List<CatalogPartItem>());

        return new AIServiceClient(httpClient, catalogMock.Object, new Mock<IPartRepository>().Object,
            breaker ?? new AICircuitBreaker(), NullLogger<AIServiceClient>.Instance);
    }

    // ───────────────────────── Case 4 — Hallucination (validate ngược thật ở .NET) ─────────────────────────

    [Fact]
    public async Task Case4_HallucinatedServiceId_IsRemovedByBackendValidation()
    {
        var knownServices = new List<CatalogServiceItem> { new("svc-real", "Vệ sinh main", "phone", 150000) };

        var fakeBody = new
        {
            requestId = Guid.NewGuid(),
            status = "SUCCESS",
            // "Thay chip ABC Super Pro" giả sử lọt qua được validate FastAPI (case cực đoan:
            // giả lập FastAPI có bug hoặc bị bypass) -> Backend VẪN PHẢI tự chặn lần nữa.
            suggestedServices = new[] { new { serviceId = "svc-chip-abc-super-pro", serviceName = "Thay chip ABC Super Pro", confidence = "HIGH" } },
            suggestedParts = Array.Empty<object>(),
            priceRange = new { min = 500000, max = 800000 },
            reason = "test", disclaimer = "test",
        };
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(fakeBody) });

        var client = BuildClient(handler, knownServices);
        var result = await client.GetAdviceAsync(new AIAdviceRequest("Phone", "iPhone", "13", "test"));

        Assert.DoesNotContain(result.SuggestedServices, s => s.ServiceId == "svc-chip-abc-super-pro");
        Assert.Equal(AIAdvisoryStatus.NoMatch, result.Status); // không còn gì hợp lệ -> NO_MATCH
    }

    // ───────────────────────── Case 6 — AI Down (FastAPI tắt hẳn -> connection refused) ─────────────────────────

    [Fact]
    public async Task Case6_AIServiceCompletelyDown_ReturnsFallback_TicketFlowUnaffected()
    {
        var handler = new ThrowingHttpMessageHandler(new HttpRequestException("Connection refused"));
        var client = BuildClient(handler);

        var result = await client.GetAdviceAsync(new AIAdviceRequest("Phone", "iPhone", "13", "test"));

        // API vẫn "200" ở tầng Controller (Handler bọc try/catch, không throw ra HTTP layer) —
        // xác nhận qua AIAdvisoryStatus.Unavailable, KHÔNG phải exception văng lên Controller.
        Assert.Equal(AIAdvisoryStatus.Unavailable, result.Status);
        Assert.Equal("Tính năng tư vấn AI tạm thời không khả dụng, vui lòng thử lại.", result.Reason);

        // Xác nhận KHÔNG có exception nào được ném ra ngoài (test này tự pass nếu không crash) —
        // đúng yêu cầu "Ticket vẫn hoạt động" vì CreateTicketCommandHandler không phụ thuộc IAIService.
    }

    // ───────────────────────── Case 7 — Timeout > 5s ─────────────────────────

    [Fact]
    public async Task Case7_ResponseSlowerThan5Seconds_ReturnsAITimeout()
    {
        var handler = new DelayedHttpMessageHandler(TimeSpan.FromSeconds(6), HttpStatusCode.OK, new { });
        var client = BuildClient(handler, timeout: TimeSpan.FromSeconds(5));

        var result = await client.GetAdviceAsync(new AIAdviceRequest("Phone", "iPhone", "13", "test"));

        Assert.Equal(AIAdvisoryStatus.Unavailable, result.Status);
    }

    // ───────────────────────── Case 8 — Invalid API Key (FastAPI trả 401) ─────────────────────────

    [Fact]
    public async Task Case8_InvalidApiKey_Returns401FromFastAPI_MappedToUnavailable_NotExposedToFrontend()
    {
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = BuildClient(handler);

        var result = await client.GetAdviceAsync(new AIAdviceRequest("Phone", "iPhone", "13", "test"));

        Assert.Equal(AIAdvisoryStatus.Unavailable, result.Status);
        // Message KHÔNG được tiết lộ lý do thật (sai API key) cho Frontend — đúng Task 6.18
        Assert.DoesNotContain("API key", result.Reason, StringComparison.OrdinalIgnoreCase);
    }
}

internal class ThrowingHttpMessageHandler : HttpMessageHandler
{
    private readonly Exception _exception;
    public ThrowingHttpMessageHandler(Exception exception) => _exception = exception;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => throw _exception;
}