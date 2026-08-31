using RepairShop.Application.Modules.Users.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Users.Queries;

public record GetUsersQuery(string? Role, bool? IsActive,
    int Page = 1, int PageSize = 50) : IRequest<UserListResponse>;