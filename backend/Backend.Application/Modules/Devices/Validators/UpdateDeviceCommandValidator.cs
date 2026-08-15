using RepairShop.Application.Modules.Devices.Commands;
using FluentValidation;

namespace RepairShop.Application.Modules.Devices.Validators;

public class UpdateDeviceCommandValidator : AbstractValidator<UpdateDeviceCommand>
{
    public UpdateDeviceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Brand).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SerialNumber).MaximumLength(100);
    }
}