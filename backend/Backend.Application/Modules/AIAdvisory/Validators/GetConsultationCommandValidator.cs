using FluentValidation;
using RepairShop.Application.Modules.AIAdvisory.Commands.GetConsultation;

namespace RepairShop.Application.Modules.AIAdvisory.Validators;

public class GetConsultationCommandValidator : AbstractValidator<GetConsultationCommand>
{
    private static readonly string[] ValidDeviceTypes = ["Phone", "Laptop", "Electronics"];

    public GetConsultationCommandValidator()
    {
        RuleFor(x => x.DeviceType)
            .NotEmpty()
            .Must(t => ValidDeviceTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage("DeviceType phải là Phone, Laptop hoặc Electronics.");

        RuleFor(x => x.Brand).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100);

        RuleFor(x => x.IssueDescription)
            .NotEmpty().WithMessage("Vui lòng mô tả tình trạng thiết bị.")
            .MaximumLength(500).WithMessage("Mô tả không vượt quá 500 ký tự.");

        RuleFor(x => x.FullName).MaximumLength(100);

        RuleFor(x => x.Phone)
            .MaximumLength(20)
            .Matches(@"^[0-9+\s.\-]{9,20}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("Số điện thoại không hợp lệ.");
    }
}
