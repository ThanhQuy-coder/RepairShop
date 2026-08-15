namespace RepairShop.Application.Modules.Identity.DTOs;

public record RegisterRequest(string FullName, string Email, string Password, string? Phone);