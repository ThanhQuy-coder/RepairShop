using RepairShop.Application.Modules.Inventory.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Inventory.Commands;

public record CreatePartCommand(
    string Name, string Sku, decimal CostPrice, decimal UnitPrice,
    string? Category, string? CompatibleDeviceType, string Unit, int MinStockThreshold)
    : IRequest<PartDetailResponse>;