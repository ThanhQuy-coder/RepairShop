using RepairShop.Application.Modules.Customers.Commands;
using FluentValidation;

namespace RepairShop.Application.Modules.Customers.Validators;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Address).MaximumLength(255);
        RuleFor(x => x.UserId).NotEmpty().When(x => x.UserId is not null);
    }
}