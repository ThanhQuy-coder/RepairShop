using RepairShop.Application.Modules.AIAdvisory.Commands.GetAIAdvice;
using FluentValidation;

namespace RepairShop.Application.Modules.AIAdvisory.Validators;

public class GetAIAdviceCommandValidator : AbstractValidator<GetAIAdviceCommand>
{
    private static readonly string[] ValidDeviceTypes = ["Phone", "Laptop", "Electronics"];

    public GetAIAdviceCommandValidator()
    {
        RuleFor(x => x.DeviceType)
            .NotEmpty()
            .Must(t => ValidDeviceTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage("DeviceType phải là Phone, Laptop hoặc Electronics.");

        RuleFor(x => x.Brand).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100);

        RuleFor(x => x.IssueDescription)
            .NotEmpty().WithMessage("Vui lòng mô tả tình trạng thiết bị.")
            .MaximumLength(500).WithMessage("Mô tả không vượt quá 500 ký tự."); // khớp giới hạn AI Contract Task 6.3
    }
}