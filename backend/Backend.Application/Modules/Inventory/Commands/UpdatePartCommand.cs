using RepairShop.Application.Modules.Inventory.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Inventory.Commands;

public record UpdatePartCommand(
    Guid Id, string Name, decimal CostPrice, decimal UnitPrice,
    string? Category, string? CompatibleDeviceType, string Unit, int MinStockThreshold)
    : IRequest<PartDetailResponse>;