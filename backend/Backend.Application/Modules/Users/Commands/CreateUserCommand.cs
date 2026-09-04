using MediatR;
using RepairShop.Application.Modules.Users.DTOs;

namespace RepairShop.Application.Modules.Users.Commands;

public record CreateUserCommand(
    string FullName,
    string Email,
    string? Phone,
    string Role,
    string Password) : IRequest<UserListItemResponse>;

public record SetUserStatusCommand(Guid Id, bool IsActive) : IRequest<UserListItemResponse>;
