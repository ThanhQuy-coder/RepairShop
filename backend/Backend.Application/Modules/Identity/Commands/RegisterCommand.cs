using Backend.Application.Modules.Identity.DTOs;
using MediatR;

namespace Backend.Application.Modules.Identity.Commands;

public record RegisterCommand(string FullName, string Email, string Password, string? Phone)
    : IRequest<AuthResponse>;