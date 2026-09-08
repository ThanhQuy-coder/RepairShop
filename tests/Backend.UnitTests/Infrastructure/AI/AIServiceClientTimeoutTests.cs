using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.AIAdvisory.DTOs;
using RepairShop.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace RepairShop.UnitTests.Infrastructure.AI;

public class AIServiceClientTimeoutTests
{
    [Fact]
    public async Task ResponseWithinFiveSeconds_ReturnsSuccess()
    {
        var handler = new DelayedHttpMessageHandler(delay: TimeSpan.FromSeconds(1), statusCode: System.Net.HttpStatusCode.OK,
            body: new { requestId = Guid.NewGuid(), status = "NO_MATCH", suggestedServices = Array.Empty<object>(),
                suggestedParts = Array.Empty<object>(), priceRange = (object?)null, reason = "test", disclaimer = (string?)null });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8000"),
            Timeout = TimeSpan.FromSeconds(5), // đúng contract Task 5 Tuần 2
        };

        var client = BuildClient(httpClient);
        var result = await client.GetAdviceAsync(new AIAdviceRequest("Phone", "iPhone", "13", "test"));

        Assert.Equal(AIAdvisoryStatus.NoMatch, result.Status); // KHÔNG bị timeout vì 1s < 5s
    }

    [Fact]
    public async Task ResponseSlowerThanFiveSeconds_ReturnsUnavailable_NotThrow()
    {
        var handler = new DelayedHttpMessageHandler(delay: TimeSpan.FromSeconds(6), statusCode: System.Net.HttpStatusCode.OK, body: new { });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8000"),
            Timeout = TimeSpan.FromSeconds(5), // HttpClient tự huỷ request sau 5s
        };

        var client = BuildClient(httpClient);
        var result = await client.GetAdviceAsync(new AIAdviceRequest("Phone", "iPhone", "13", "test"));

        Assert.Equal(AIAdvisoryStatus.Unavailable, result.Status);
        Assert.Equal("Tính năng tư vấn AI tạm thời không khả dụng, vui lòng thử lại.", result.Reason);
    }

    private static AIServiceClient BuildClient(HttpClient httpClient)
    {
        var catalogMock = new Mock<IServiceCatalogQueryService>();
        catalogMock.Setup(c => c.GetAvailableServicesAsync(It.IsAny<string>())).ReturnsAsync(new List<CatalogServiceItem>());
        catalogMock.Setup(c => c.GetAvailablePartsAsync()).ReturnsAsync(new List<CatalogPartItem>());

        return new AIServiceClient(httpClient, catalogMock.Object, new Mock<IPartRepository>().Object,
            new AICircuitBreaker(), NullLogger<AIServiceClient>.Instance);
    }
}

internal class DelayedHttpMessageHandler : HttpMessageHandler
{
    private readonly TimeSpan _delay;
    private readonly System.Net.HttpStatusCode _statusCode;
    private readonly object _body;

    public DelayedHttpMessageHandler(TimeSpan delay, System.Net.HttpStatusCode statusCode, object body)
    {
        _delay = delay;
        _statusCode = statusCode;
        _body = body;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await Task.Delay(_delay, cancellationToken); // ném TaskCanceledException nếu bị huỷ giữa chừng do HttpClient.Timeout
        return new HttpResponseMessage(_statusCode) { Content = System.Net.Http.Json.JsonContent.Create(_body) };
    }
}