using FluentValidation;

public class UsePartCommandValidator : AbstractValidator<UsePartCommand>
{
    public UsePartCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.PartId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}