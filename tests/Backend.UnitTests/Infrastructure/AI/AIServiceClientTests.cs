using System.Net;
using System.Net.Http.Json;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.AIAdvisory.DTOs;
using RepairShop.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using RepairShop.Domain.Modules.Inventory;
using Microsoft.Extensions.Logging;

namespace RepairShop.UnitTests.Infrastructure.AI;

public class AIServiceClientTests
{
    private static AIServiceClient CreateClient(HttpResponseMessage response, List<CatalogPartItem>? parts = null)
    {
        var handler = new FakeHttpMessageHandler(response);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") };
        var partRepoMock = new Mock<IPartRepository>();
        var circuitBreakerMock = new Mock<IAICircuitBreaker>();

        var catalogMock = new Mock<IServiceCatalogQueryService>();
        catalogMock.Setup(c => c.GetAvailableServicesAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<CatalogServiceItem>());
        catalogMock.Setup(c => c.GetAvailablePartsAsync())
            .ReturnsAsync(parts ?? new List<CatalogPartItem>());

        return new AIServiceClient(httpClient, catalogMock.Object, partRepoMock.Object, circuitBreakerMock.Object, NullLogger<AIServiceClient>.Instance);
    }

    [Fact]
    public async Task GetAdviceAsync_SuccessResponse_MapsToSuccessResult()
    {
        var fakeBody = new
        {
            requestId = Guid.NewGuid(),
            status = "SUCCESS",
            suggestedServices = new[] { new { serviceId = "svc-1", serviceName = "Thay pin", confidence = "HIGH" } },
            suggestedParts = new[] { new { partId = "part-1", partName = "Pin iPhone 13" } },
            priceRange = new { min = 300000, max = 500000 },
            reason = "test reason",
            disclaimer = "test disclaimer",
        };
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(fakeBody) };

        var client = CreateClient(httpResponse);
        var result = await client.GetAdviceAsync(new AIAdviceRequest("Phone", "iPhone", "13", "Pin tụt nhanh"));

        Assert.Equal(AIAdvisoryStatus.Success, result.Status);
        Assert.Single(result.SuggestedServices);
        Assert.Equal("svc-1", result.SuggestedServices[0].ServiceId);
    }

    [Fact]
    public async Task GetAdviceAsync_ErrorStatusCode_ReturnsUnavailable_NotThrow()
    {
        var httpResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var client = CreateClient(httpResponse);

        var result = await client.GetAdviceAsync(new AIAdviceRequest("Phone", "iPhone", "13", "Pin tụt nhanh"));

        Assert.Equal(AIAdvisoryStatus.Unavailable, result.Status); // KHÔNG throw exception làm gãy luồng chính
    }

    [Fact]
    public async Task GetAdviceAsync_AIHallucinatesUnknownServiceId_IsDiscarded()
    {
        var knownParts = new List<CatalogPartItem> { new("real-part-id", "Pin iPhone 13", 350000) };

        var fakeBody = new
        {
            requestId = Guid.NewGuid(),
            status = "SUCCESS",
            suggestedServices = new[] { new { serviceId = "svc-does-not-exist", serviceName = "Thay IC XYZ", confidence = "HIGH" } },
            suggestedParts = new[] { new { partId = "real-part-id", partName = "Pin iPhone 13" } },
            priceRange = new { min = 300000, max = 500000 },
            reason = "test",
            disclaimer = "test",
        };
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(fakeBody) };

        var partRepoMock = new Mock<IPartRepository>();
        partRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Part?)null); // partId không phải Guid hợp lệ -> bỏ qua re-verify, giữ theo catalog

        var handler = new FakeHttpMessageHandler(httpResponse);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") };

        var catalogMock = new Mock<IServiceCatalogQueryService>();
        catalogMock.Setup(c => c.GetAvailableServicesAsync(It.IsAny<string>())).ReturnsAsync(new List<CatalogServiceItem>()); // KHÔNG có service nào thật
        catalogMock.Setup(c => c.GetAvailablePartsAsync()).ReturnsAsync(knownParts);

        var circuitBreakerMock = new Mock<IAICircuitBreaker>();

        var client = new AIServiceClient(
            httpClient,
            catalogMock.Object,
            partRepoMock.Object,
            circuitBreakerMock.Object,
            NullLogger<AIServiceClient>.Instance);

        var result = await client.GetAdviceAsync(new AIAdviceRequest("Phone", "iPhone", "13", "test"));

        Assert.DoesNotContain(result.SuggestedServices, s => s.ServiceId == "svc-does-not-exist");
        Assert.Empty(result.SuggestedServices); // "Thay IC XYZ" bị loại hoàn toàn — đúng ví dụ mentor đưa
    }
}

internal class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;
    public FakeHttpMessageHandler(HttpResponseMessage response) => _response = response;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_response);
}