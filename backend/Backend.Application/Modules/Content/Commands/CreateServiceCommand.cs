using RepairShop.Application.Modules.Content.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Content.Commands;

public record CreateServiceCommand(string Name, string? Description, decimal? BasePrice, string? DeviceType)
    : IRequest<ServiceResponse>;