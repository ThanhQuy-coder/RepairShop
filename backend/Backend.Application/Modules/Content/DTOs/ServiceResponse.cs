namespace RepairShop.Application.Modules.Content.DTOs;

public record ServiceResponse(Guid Id, string Name, string? Description, decimal? BasePrice, string? DeviceType, bool IsActive);
public record ServiceListResponse(List<ServiceResponse> Items, int Total);