using FluentValidation;

public class CreateWarrantyCommandValidator : AbstractValidator<CreateWarrantyCommand>
{
    public CreateWarrantyCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.WarrantyMonths).GreaterThan(0).LessThanOrEqualTo(36)
            .WithMessage("Thời hạn bảo hành phải từ 1-36 tháng.");
        RuleFor(x => x.Terms).MaximumLength(500);
    }
}