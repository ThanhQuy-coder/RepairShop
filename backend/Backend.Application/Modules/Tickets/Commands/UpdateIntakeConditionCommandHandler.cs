using MediatR;
using RepairShop.Application.Common.Interfaces;

namespace RepairShop.Application.Modules.Tickets.Commands;

public class UpdateIntakeConditionCommandHandler : IRequestHandler<UpdateIntakeConditionCommand, Unit>
{
    private readonly IRepairTicketRepository _ticketRepository;

    public UpdateIntakeConditionCommandHandler(IRepairTicketRepository ticketRepository) =>
        _ticketRepository = ticketRepository;

    public async Task<Unit> Handle(UpdateIntakeConditionCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new RepairShop.Application.Common.Exceptions.NotFoundException("Phiếu sửa chữa", request.TicketId);

        ticket.RecordIntakeCondition(request.ConditionNotes, request.RiskWarning); // tự chặn nếu đã qua CHECKED_IN

        await _ticketRepository.SaveChangesAsync();
        return Unit.Value;
    }
}