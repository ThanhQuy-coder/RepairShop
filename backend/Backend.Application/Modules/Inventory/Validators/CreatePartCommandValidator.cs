using RepairShop.Application.Modules.Inventory.Commands;
using FluentValidation;

namespace RepairShop.Application.Modules.Inventory.Validators;

public class CreatePartCommandValidator : AbstractValidator<CreatePartCommand>
{
    public CreatePartCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinStockThreshold).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.CompatibleDeviceType)
            .Must(t => t == null || new[] { "Phone", "Laptop", "Electronics" }.Contains(t))
            .WithMessage("CompatibleDeviceType phải là Phone, Laptop, Electronics hoặc để trống (dùng chung).");
    }
}

public class UpdatePartCommandValidator : AbstractValidator<UpdatePartCommand>
{
    public UpdatePartCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinStockThreshold).GreaterThanOrEqualTo(0);
    }
}