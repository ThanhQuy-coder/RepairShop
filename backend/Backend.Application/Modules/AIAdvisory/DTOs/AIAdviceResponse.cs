namespace RepairShop.Application.Modules.AIAdvisory.DTOs;

// Response trả cho Frontend — khớp đúng contract Task 4 mục 13.1 Tuần 2
public record AIAdviceServiceItem(string ServiceId, string ServiceName, decimal PriceRangeMin, decimal PriceRangeMax);

public record AIAdviceResponse(
    bool AiAvailable,
    List<AIAdviceServiceItem> SuggestedServices,
    List<string> SuggestedParts,
    string? Explanation,
    string? Message,
    string Disclaimer);