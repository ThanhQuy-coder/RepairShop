using MediatR;
using RepairShop.Application.Common.Authorization;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;

public class RecordCompletionNotesCommandHandler : IRequestHandler<RecordCompletionNotesCommand, Unit>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly ICurrentUserService _currentUser;

    public RecordCompletionNotesCommandHandler(IRepairTicketRepository ticketRepository, ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(RecordCompletionNotesCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        TicketAccessGuard.EnsureCanAccess(ticket, _currentUser);

        ticket.RecordCompletionNotes(request.CompletionNotes);
        await _ticketRepository.SaveChangesAsync();
        return Unit.Value;
    }
}