using RepairShop.Application.Modules.Appointments.Commands;
using FluentValidation;

namespace RepairShop.Application.Modules.Appointments.Validators;

public class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.TimeSlotId).NotEmpty();
        RuleFor(x => x.AppointmentDate).GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Ngày hẹn không thể ở quá khứ.");
        RuleFor(x => x.IssueDescription).MaximumLength(500);
        RuleFor(x => x.ParentTicketId)
            .NotEmpty()
            .When(x => x.Type == Domain.Modules.Appointments.Enums.AppointmentType.WarrantyRevisit)
            .WithMessage("Lịch hẹn bảo hành phải liên kết phiếu gốc.");
    }
}