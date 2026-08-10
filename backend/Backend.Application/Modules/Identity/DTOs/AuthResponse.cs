namespace Backend.Application.Modules.Identity.DTOs;

public record AuthResponse(string AccessToken, int ExpiresIn, string Role, string Email);