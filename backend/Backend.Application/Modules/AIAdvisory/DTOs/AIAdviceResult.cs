namespace RepairShop.Application.Modules.AIAdvisory.DTOs;

public record AISuggestedService(string ServiceId, string ServiceName, string Confidence);
public record AISuggestedPart(string PartId, string PartName);
public record AIPriceRange(decimal Min, decimal Max);

public enum AIAdvisoryStatus { Success, NoMatch, OutOfScope, Unavailable }

// Unavailable: dùng khi AI Service timeout/lỗi — KHÔNG throw exception làm gãy luồng chính
// (đúng nguyên tắc "AI là tính năng hỗ trợ, non-critical" đã chốt Task 5 Tuần 2).
public record AIAdviceResult(
    AIAdvisoryStatus Status,
    List<AISuggestedService> SuggestedServices,
    List<AISuggestedPart> SuggestedParts,
    AIPriceRange? PriceRange,
    string Reason,
    string? Disclaimer);