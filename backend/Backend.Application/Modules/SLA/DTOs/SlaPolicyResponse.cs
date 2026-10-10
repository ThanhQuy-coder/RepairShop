using RepairShop.Domain.Common.Enums;

namespace RepairShop.Application.Modules.SLA.DTOs;

public sealed record SlaPolicyResponse(
    Guid Id,
    string StatusCode,
    DeviceType? DeviceType,
    int DurationMinutes,
    bool IsActive);
