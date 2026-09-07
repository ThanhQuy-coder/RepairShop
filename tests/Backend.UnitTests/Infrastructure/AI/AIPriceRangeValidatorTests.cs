using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.AIAdvisory.DTOs;
using RepairShop.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace RepairShop.UnitTests.Infrastructure.AI;

public class AIPriceRangeValidatorTests
{
    private static readonly List<CatalogServiceItem> KnownServices =
        [new("svc-1", "Thay pin", "phone", 400000)];
    private static readonly List<AISuggestedService> ValidatedServices =
        [new("svc-1", "Thay pin", "HIGH")];

    [Fact]
    public void ExtremelyWidePriceRange_ComparedToBasePrice_IsDiscarded()
    {
        // Đúng ví dụ mentor: AI trả 100.000 -> 50.000.000, basePrice thật = 400.000
        var priceRange = new AIPriceRange(100_000, 50_000_000);

        var result = AIPriceRangeValidator.ValidateOrDiscard(
            priceRange, ValidatedServices, [], KnownServices, [], NullLogger.Instance);

        Assert.Null(result); // bị loại bỏ vì lệch quá 50% so với 400.000
    }

    [Fact]
    public void ReasonablePriceRange_WithinFiftyPercent_IsKept()
    {
        // basePrice = 400.000, khoảng cho phép [200.000 - 600.000]
        var priceRange = new AIPriceRange(350_000, 500_000);

        var result = AIPriceRangeValidator.ValidateOrDiscard(
            priceRange, ValidatedServices, [], KnownServices, [], NullLogger.Instance);

        Assert.NotNull(result);
        Assert.Equal(350_000, result!.Min);
    }

    [Fact]
    public void MinGreaterThanMax_IsDiscarded()
    {
        var priceRange = new AIPriceRange(500_000, 300_000);

        var result = AIPriceRangeValidator.ValidateOrDiscard(
            priceRange, ValidatedServices, [], KnownServices, [], NullLogger.Instance);

        Assert.Null(result);
    }

    [Fact]
    public void NoReferencePrice_Available_IsDiscarded()
    {
        var priceRange = new AIPriceRange(100_000, 200_000);

        var result = AIPriceRangeValidator.ValidateOrDiscard(
            priceRange, [], [], [], [], NullLogger.Instance);

        Assert.Null(result); // không có gì để đối chiếu -> an toàn là loại bỏ
    }
}