using Backend.Application.Modules.Identity.DTOs;
using MediatR;

namespace Backend.Application.Modules.Identity.Commands;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;