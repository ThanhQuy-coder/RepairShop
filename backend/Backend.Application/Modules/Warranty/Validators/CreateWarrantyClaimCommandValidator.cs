using RepairShop.Application.Modules.Warranty.Commands;
using FluentValidation;

namespace RepairShop.Application.Modules.Warranty.Validators;

public class CreateWarrantyClaimCommandValidator : AbstractValidator<CreateWarrantyClaimCommand>
{
    public CreateWarrantyClaimCommandValidator()
    {
        RuleFor(x => x.ParentTicketId).NotEmpty();
        RuleFor(x => x.IssueReported).NotEmpty().WithMessage("Vui lòng mô tả vấn đề gặp phải.").MaximumLength(1000);
    }
}