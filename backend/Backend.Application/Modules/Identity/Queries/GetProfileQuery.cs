using Backend.Application.Modules.Identity.DTOs;
using MediatR;

namespace Backend.Application.Modules.Identity.Queries;

// Không cần truyền Id — Handler tự lấy từ ICurrentUserService (JWT của chính người gọi)
public record GetProfileQuery : IRequest<UserProfileResponse>;