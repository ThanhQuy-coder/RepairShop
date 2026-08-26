using RepairShop.Application.Modules.Tickets.Queries;
using FluentValidation;

namespace RepairShop.Application.Modules.Tickets.Validators;

public class TrackTicketByCodeQueryValidator : AbstractValidator<TrackTicketByCodeQuery>
{
    public TrackTicketByCodeQueryValidator()
    {
        RuleFor(x => x.TicketCode)
            .NotEmpty()
            .Matches(@"^RT-\d{8}-\d{4,}$") // khớp format TicketCodeGenerator (Task 4.3): RT-yyyyMMdd-xxxx
            .WithMessage("Mã phiếu không đúng định dạng.");
    }
}