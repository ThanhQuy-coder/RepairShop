using System.Net;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.AIAdvisory.DTOs;
using RepairShop.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace RepairShop.UnitTests.Infrastructure.AI;

public class AIServiceClientErrorHandlingTests
{
    private static AIServiceClient CreateClient(HttpStatusCode statusCode, IAICircuitBreaker? breaker = null)
    {
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage(statusCode));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") };

        var catalogMock = new Mock<IServiceCatalogQueryService>();
        catalogMock.Setup(c => c.GetAvailableServicesAsync(It.IsAny<string>())).ReturnsAsync(new List<CatalogServiceItem>());
        catalogMock.Setup(c => c.GetAvailablePartsAsync()).ReturnsAsync(new List<CatalogPartItem>());

        var partRepoMock = new Mock<IPartRepository>();

        return new AIServiceClient(httpClient, catalogMock.Object, partRepoMock.Object,
            breaker ?? new AICircuitBreaker(), NullLogger<AIServiceClient>.Instance);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task AllErrorStatusCodes_ReturnUnavailable_NeverThrow(HttpStatusCode statusCode)
    {
        var client = CreateClient(statusCode);

        var result = await client.GetAdviceAsync(new AIAdviceRequest("Phone", "iPhone", "13", "test"));

        Assert.Equal(AIAdvisoryStatus.Unavailable, result.Status);
        Assert.Equal("Tính năng tư vấn AI tạm thời không khả dụng, vui lòng thử lại.", result.Reason);
    }
}