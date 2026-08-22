using RepairShop.Application.Modules.Tickets.Commands;
using FluentValidation;

namespace RepairShop.Application.Modules.Tickets.Validators;

public class AssignTechnicianCommandValidator : AbstractValidator<AssignTechnicianCommand>
{
    public AssignTechnicianCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.TechnicianId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(255);
    }
}