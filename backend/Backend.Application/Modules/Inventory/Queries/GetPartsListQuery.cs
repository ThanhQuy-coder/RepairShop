using RepairShop.Application.Modules.Inventory.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Inventory.Queries;

public record GetPartsListQuery(string? Search, string? Category, int Page = 1, int PageSize = 20)
    : IRequest<PartListResponse>;