using FluentValidation;

public class FailQualityCheckCommandValidator : AbstractValidator<FailQualityCheckCommand>
{
    public FailQualityCheckCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.FailureReason).NotEmpty().WithMessage("Phải ghi lý do QA không đạt.").MaximumLength(500);
    }
}