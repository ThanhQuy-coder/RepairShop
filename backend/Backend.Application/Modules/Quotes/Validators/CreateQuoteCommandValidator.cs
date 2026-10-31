using FluentValidation;

public class CreateQuoteCommandValidator : AbstractValidator<CreateQuoteCommand>
{
    public CreateQuoteCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Items).NotEmpty().WithMessage("Báo giá phải có ít nhất 1 hạng mục.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.ItemType).Must(t => t is "Service" or "Part")
                .WithMessage("ItemType phải là 'Service' hoặc 'Part'.");
            item.RuleFor(i => i)
                .Must(i => i.ItemType == "Service" && i.ServiceId.HasValue && !i.PartId.HasValue ||
                           i.ItemType == "Part" && i.PartId.HasValue && !i.ServiceId.HasValue)
                .WithMessage("Mỗi hạng mục phải liên kết đúng một ServiceId hoặc PartId.");
        });
    }
}