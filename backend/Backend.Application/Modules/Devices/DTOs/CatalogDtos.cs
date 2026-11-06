namespace RepairShop.Application.Modules.Devices.DTOs;

public record BrandResponse(
    Guid Id,
    string Name,
    string Code,
    string? LogoUrl,
    string? Description,
    bool IsActive,
    List<string> DeviceTypes,
    int ModelCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record BrandListResponse(List<BrandResponse> Items, int Total);

public record DeviceModelResponse(
    Guid Id,
    Guid BrandId,
    string BrandName,
    string BrandCode,
    string DeviceType,
    string ModelName,
    string? ModelNumber,
    int? ReleaseYear,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record DeviceModelListResponse(List<DeviceModelResponse> Items, int Total);
