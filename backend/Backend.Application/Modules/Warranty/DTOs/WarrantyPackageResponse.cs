namespace RepairShop.Application.Modules.Warranty.DTOs;

public record WarrantyPackageResponse(
    Guid Id, string Name, string? Description, int DurationMonths, string Terms, bool IsActive);
