using MediatR;

namespace RepairShop.Application.Modules.Reviews.Commands;

public record CreateReviewCommand(Guid TicketId, int Rating, string? Comment) : IRequest<ReviewResponse>;
public record ReviewResponse(Guid Id, int Rating, string? Comment, DateTime CreatedAt);