using RepairShop.Application.Modules.Devices.Commands;
using FluentValidation;

namespace RepairShop.Application.Modules.Devices.Validators;

public class CreateDeviceCommandValidator : AbstractValidator<CreateDeviceCommand>
{
    public CreateDeviceCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.DeviceType).NotEmpty()
            .Must(t => new[] { "Phone", "Laptop", "Electronics" }.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage("DeviceType phải là Phone, Laptop hoặc Electronics.");
        RuleFor(x => x.Brand).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SerialNumber).MaximumLength(100);
    }
}