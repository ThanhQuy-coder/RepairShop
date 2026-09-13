using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.AIAdvisory.DTOs;
using RepairShop.Domain.Modules.Inventory;
using RepairShop.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace RepairShop.UnitTests.Infrastructure.AI;

public class AIServiceClientTests
{
    /// <summary>
    /// Helper dựng AIServiceClient đúng constructor HIỆN TẠI (5 tham số, sau Task 6.16/6.18).
    /// Cho phép truyền knownServices/knownParts để test Validate-ngược hoạt động đúng —
    /// nếu không truyền, mặc định rỗng (đúng behavior "không có gì để đối chiếu -> NoMatch").
    /// </summary>
    private static AIServiceClient CreateClient(
        HttpResponseMessage response,
        List<CatalogServiceItem>? knownServices = null,
        List<CatalogPartItem>? knownParts = null,
        IAICircuitBreaker? breaker = null)
    {
        var handler = new FakeHttpMessageHandler(response);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") };

        var catalogMock = new Mock<IServiceCatalogQueryService>();
        catalogMock.Setup(c => c.GetAvailableServicesAsync(It.IsAny<string>()))
            .ReturnsAsync(knownServices ?? new List<CatalogServiceItem>());
        catalogMock.Setup(c => c.GetAvailablePartsAsync())
            .ReturnsAsync(knownParts ?? new List<CatalogPartItem>());

        // QUAN TRỌNG: phải Setup tường minh cho GetByIdAsync, vì AIServiceClient gọi
        // await _partRepository.GetByIdAsync(...) khi re-verify Part trong ValidateAndMapAsync.
        // Nếu không Setup, Moq mặc định trả về Task với giá trị default (null cho Part?),
        // khiến logic "freshPart is null -> discard" luôn kích hoạt sai, làm rỗng hết suggestion.
        var partRepoMock = new Mock<IPartRepository>();
        partRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new Part("Pin iPhone 13", "SKU-001", 200000m, 350000m, null, null)); // trả về Part "tồn tại" cho mọi Guid hợp lệ

        return new AIServiceClient(
            httpClient,
            catalogMock.Object,
            partRepoMock.Object,
            breaker ?? new AICircuitBreaker(),
            NullLogger<AIServiceClient>.Instance);
    }

    [Fact]
    public async Task GetAdviceAsync_SuccessResponse_MapsToSuccessResult()
    {
        var fakeBody = new
        {
            requestId = Guid.NewGuid(),
            status = "SUCCESS",
            suggestedServices = new[] { new { serviceId = "svc-1", serviceName = "Thay pin", confidence = "HIGH" } },
            suggestedParts = Array.Empty<object>(),
            priceRange = new { min = 300000, max = 500000 },
            reason = "test reason",
            disclaimer = "test disclaimer",
        };
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(fakeBody) };

        // ĐIỂM MẤU CHỐT của fix: khai báo knownServices/knownParts KHỚP đúng ID mà fakeBody
        // trả về — nếu không, bước Validate ngược (Task 6.16) sẽ coi svc-1/part-1 là
        // hallucination và loại bỏ hết, khiến kết quả rơi về NoMatch/Unavailable thay vì Success.
        var knownServices = new List<CatalogServiceItem> { new("svc-1", "Thay pin", "phone", 400000) };
        var client = CreateClient(httpResponse, knownServices);
        var result = await client.GetAdviceAsync(new AIAdviceRequest("Phone", "iPhone", "13", "Pin tụt nhanh"));

        Assert.Equal(AIAdvisoryStatus.Success, result.Status);
        Assert.Single(result.SuggestedServices);
        Assert.Equal("svc-1", result.SuggestedServices[0].ServiceId);
        Assert.NotNull(result.PriceRange);
    }

    [Fact]
    public async Task GetAdviceAsync_ErrorStatusCode_ReturnsUnavailable_NotThrow()
    {
        var httpResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var client = CreateClient(httpResponse);

        var result = await client.GetAdviceAsync(new AIAdviceRequest("Phone", "iPhone", "13", "Pin tụt nhanh"));

        Assert.Equal(AIAdvisoryStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task GetAdviceAsync_UnknownServiceId_NotInCatalog_IsDiscarded()
    {
        // Trường hợp catalog RỖNG (khớp đúng ý đồ ban đầu bạn viết ở Task 6.13) — giờ kết quả
        // đúng phải là NoMatch (không phải Success, và cũng không phải Unavailable).
        var fakeBody = new
        {
            requestId = Guid.NewGuid(),
            status = "SUCCESS",
            suggestedServices = new[] { new { serviceId = "svc-1", serviceName = "Thay pin", confidence = "HIGH" } },
            suggestedParts = Array.Empty<object>(),
            priceRange = new { min = 300000, max = 500000 },
            reason = "test",
            disclaimer = "test",
        };
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(fakeBody) };

        var client = CreateClient(httpResponse); // knownServices/knownParts mặc định rỗng
        var result = await client.GetAdviceAsync(new AIAdviceRequest("Phone", "iPhone", "13", "test"));

        Assert.Equal(AIAdvisoryStatus.NoMatch, result.Status);
        Assert.Empty(result.SuggestedServices);
    }
}

internal class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;
    public FakeHttpMessageHandler(HttpResponseMessage response) => _response = response;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_response);
}