using FluentValidation;

public class CreateWarrantyCommandValidator : AbstractValidator<CreateWarrantyCommand>
{
    public CreateWarrantyCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.WarrantyPackageId).NotEmpty();
    }
}