using RepairShop.Application.Modules.Identity.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Identity.Commands;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;