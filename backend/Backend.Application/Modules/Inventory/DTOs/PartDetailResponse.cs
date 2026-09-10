namespace RepairShop.Application.Modules.Inventory.DTOs;

public record PartDetailResponse(
    Guid Id, string Name, string Sku, string? Category, string? CompatibleDeviceType,
    decimal CostPrice, decimal UnitPrice, string Unit, int MinStockThreshold,
    int QuantityOnHand, bool IsLowStock, bool IsActive, DateTime CreatedAt);

public record PartListResponse(List<PartDetailResponse> Items, int Total);