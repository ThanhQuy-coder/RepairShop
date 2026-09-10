using RepairShop.Application.Modules.Inventory.Commands;
using FluentValidation;

namespace RepairShop.Application.Modules.Inventory.Validators;

public class CreateInventoryTransactionCommandValidator : AbstractValidator<CreateInventoryTransactionCommand>
{
    public CreateInventoryTransactionCommandValidator()
    {
        RuleFor(x => x.PartId).NotEmpty();
        RuleFor(x => x.Type).NotEmpty().Must(t => t is "Import" or "Adjustment")
            .WithMessage("Type phải là 'Import' hoặc 'Adjustment'.");
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}