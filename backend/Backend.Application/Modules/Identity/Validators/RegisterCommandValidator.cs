using RepairShop.Application.Modules.Identity.Commands;
using FluentValidation;

namespace RepairShop.Application.Modules.Identity.Validators;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống.")
            .MaximumLength(100).WithMessage("Họ tên không vượt quá 100 ký tự.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không đúng định dạng.")
            .MaximumLength(150);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.")
            .MinimumLength(6).WithMessage("Mật khẩu phải có ít nhất 6 ký tự.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Số điện thoại không được để trống.")
            .MaximumLength(20).WithMessage("Số điện thoại không vượt quá 20 ký tự.");
    }
}