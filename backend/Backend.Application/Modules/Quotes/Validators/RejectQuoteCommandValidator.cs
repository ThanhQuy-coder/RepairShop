using FluentValidation;

public class RejectQuoteCommandValidator : AbstractValidator<RejectQuoteCommand>
{
    public RejectQuoteCommandValidator()
    {
        RuleFor(x => x.QuoteId).NotEmpty();
        RuleFor(x => x.RejectReason).NotEmpty().WithMessage("Phải nêu lý do khi từ chối báo giá.").MaximumLength(500);
    }
}