using FluentValidation;
using RepairShop.Application.Modules.Appointments.Commands;

namespace RepairShop.Application.Modules.Appointments.Validators;

public class CreateTimeSlotCommandValidator : AbstractValidator<CreateTimeSlotCommand>
{
    public CreateTimeSlotCommandValidator()
    {
        RuleFor(x => x.DayOfWeek).InclusiveBetween(0, 6).When(x => x.DayOfWeek is not null);
        RuleFor(x => x.MaxCapacity).GreaterThan(0).WithMessage("MaxCapacity phải lớn hơn 0.");
        RuleFor(x => x).Must(x => x.SlotEnd > x.SlotStart).WithMessage("SlotEnd phải sau SlotStart.");
    }
}