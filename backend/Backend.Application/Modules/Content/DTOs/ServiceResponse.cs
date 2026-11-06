namespace RepairShop.Application.Modules.Content.DTOs;

public record ServiceResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal? BasePrice,
    string? DeviceType,
    bool IsActive,
    string? ImageUrl = null,
    double AverageRating = 0,
    int ReviewCount = 0);

public record ServiceListResponse(List<ServiceResponse> Items, int Total);
