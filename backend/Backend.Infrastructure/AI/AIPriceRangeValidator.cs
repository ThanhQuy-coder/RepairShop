using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.AIAdvisory.DTOs;
using Microsoft.Extensions.Logging;

namespace RepairShop.Infrastructure.AI;

/// <summary>
/// Task 6.17: kiểm tra priceRange AI trả về so với basePrice/unitPrice THẬT của các
/// service/part đã được validate (Task 6.16). Ngưỡng lệch quá 50% (contract Task 5 Tuần 2,
/// mục 6) -> loại bỏ priceRange, KHÔNG hiển thị con số sai lệch cho người dùng.
/// </summary>
public static class AIPriceRangeValidator
{
    private const decimal AllowedDeviationRatio = 0.5m; // 50%

    public static AIPriceRange? ValidateOrDiscard(
        AIPriceRange? priceRange,
        List<AISuggestedService> validatedServices,
        List<AISuggestedPart> validatedParts,
        List<CatalogServiceItem> knownServices,
        List<CatalogPartItem> knownParts,
        ILogger logger)
    {
        if (priceRange is null)
            return null;

        if (priceRange.Min > priceRange.Max)
        {
            logger.LogWarning("PriceRange không hợp lệ: min ({Min}) > max ({Max}) — loại bỏ.",
                priceRange.Min, priceRange.Max);
            return null;
        }

        var referencePrice = ComputeReferencePrice(validatedServices, validatedParts, knownServices, knownParts);

        if (referencePrice <= 0)
        {
            // Không có gì để đối chiếu (VD toàn bộ suggestion đã bị loại ở Task 6.16) —
            // không thể xác nhận priceRange hợp lý hay không -> loại bỏ để an toàn.
            logger.LogWarning("Không có giá tham chiếu nào để đối chiếu priceRange — loại bỏ.");
            return null;
        }

        var lowerBound = referencePrice * (1 - AllowedDeviationRatio);
        var upperBound = referencePrice * (1 + AllowedDeviationRatio);

        var isOutOfBounds = priceRange.Min < lowerBound || priceRange.Max > upperBound;

        if (isOutOfBounds)
        {
            logger.LogWarning(
                "PriceRange AI [{Min}-{Max}] lệch quá 50% so với giá tham chiếu {ReferencePrice} " +
                "(cho phép [{LowerBound}-{UpperBound}]) — đã loại bỏ khỏi kết quả trả về.",
                priceRange.Min, priceRange.Max, referencePrice, lowerBound, upperBound);
            return null;
        }

        return priceRange;
    }

    private static decimal ComputeReferencePrice(
        List<AISuggestedService> services, List<AISuggestedPart> parts,
        List<CatalogServiceItem> knownServices, List<CatalogPartItem> knownParts)
    {
        var serviceById = knownServices.ToDictionary(s => s.ServiceId);
        var partById = knownParts.ToDictionary(p => p.PartId);

        var total = services.Sum(s => serviceById.TryGetValue(s.ServiceId, out var known) ? known.BasePrice : 0m)
                  + parts.Sum(p => partById.TryGetValue(p.PartId, out var known) ? known.UnitPrice : 0m);

        return total;
    }
}