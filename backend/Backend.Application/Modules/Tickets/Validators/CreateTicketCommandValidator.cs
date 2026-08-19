using RepairShop.Application.Modules.Tickets.Commands;
using FluentValidation;

namespace RepairShop.Application.Modules.Tickets.Validators;

public class CreateTicketCommandValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("CustomerId không được để trống.");
        RuleFor(x => x.DeviceId).NotEmpty().WithMessage("DeviceId không được để trống.");
        RuleFor(x => x.IssueDescription)
            .NotEmpty().WithMessage("Mô tả lỗi không được để trống.")
            .MaximumLength(1000).WithMessage("Mô tả lỗi không vượt quá 1000 ký tự.");
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}