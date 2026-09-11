using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using MediatR;

namespace RepairShop.Application.Modules.Reviews.Commands;

public record ToggleReviewVisibilityCommand(Guid Id, bool IsVisible) : IRequest<Unit>;

public class ToggleReviewVisibilityCommandHandler : IRequestHandler<ToggleReviewVisibilityCommand, Unit>
{
    private readonly IReviewRepository _reviewRepository;
    public ToggleReviewVisibilityCommandHandler(IReviewRepository reviewRepository) => _reviewRepository = reviewRepository;

    public async Task<Unit> Handle(ToggleReviewVisibilityCommand request, CancellationToken cancellationToken)
    {
        var review = await _reviewRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Đánh giá", request.Id);

        if (request.IsVisible) review.Show(); else review.Hide();

        _reviewRepository.Update(review);
        await _reviewRepository.SaveChangesAsync();
        return Unit.Value;
    }
}