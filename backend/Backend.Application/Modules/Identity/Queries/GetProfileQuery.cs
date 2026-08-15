using RepairShop.Application.Modules.Identity.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Identity.Queries;

// Không cần truyền Id — Handler tự lấy từ ICurrentUserService (JWT của chính người gọi)
public record GetProfileQuery : IRequest<UserProfileResponse>;