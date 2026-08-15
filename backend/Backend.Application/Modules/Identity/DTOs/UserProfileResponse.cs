namespace RepairShop.Application.Modules.Identity.DTOs;

public record UserProfileResponse(Guid Id, string FullName, string Email, string? Phone, string Role, bool IsActive);