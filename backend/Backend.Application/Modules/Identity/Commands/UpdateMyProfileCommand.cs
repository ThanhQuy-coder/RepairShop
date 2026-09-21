using MediatR;
using RepairShop.Application.Modules.Identity.DTOs;

namespace RepairShop.Application.Modules.Identity.Commands;

public record UpdateMyProfileCommand(string FullName, string? Phone) : IRequest<UserProfileResponse>;
