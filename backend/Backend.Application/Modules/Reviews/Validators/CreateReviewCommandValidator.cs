using RepairShop.Application.Modules.Reviews.Commands;
using FluentValidation;

namespace RepairShop.Application.Modules.Reviews.Validators;

public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).WithMessage("Đánh giá phải từ 1 đến 5 sao.");
        RuleFor(x => x.Comment).MaximumLength(500);
    }
}