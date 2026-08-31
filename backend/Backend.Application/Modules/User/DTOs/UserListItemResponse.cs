namespace RepairShop.Application.Modules.Users.DTOs;

public record UserListItemResponse(Guid Id, string FullName, string Email,
    string Role, bool IsActive);
public record UserListResponse(List<UserListItemResponse> Items, int Total);