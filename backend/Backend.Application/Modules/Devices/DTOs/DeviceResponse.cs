namespace Backend.Application.Modules.Devices.DTOs;

public record DeviceResponse(Guid Id, Guid CustomerId, string DeviceType, string Brand, string Model,
    string? SerialNumber, DateTime CreatedAt);