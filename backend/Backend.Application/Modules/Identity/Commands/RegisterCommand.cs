using RepairShop.Application.Modules.Identity.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Identity.Commands;

public record RegisterCommand(string FullName, string Email, string Password, string? Phone)
    : IRequest<AuthResponse>;