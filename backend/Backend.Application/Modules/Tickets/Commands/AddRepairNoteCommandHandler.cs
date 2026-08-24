using MediatR;
using RepairShop.Application.Common.Authorization;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;

public class AddRepairNoteCommandHandler : IRequestHandler<AddRepairNoteCommand, Unit>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly ICurrentUserService _currentUser;

    public AddRepairNoteCommandHandler(IRepairTicketRepository ticketRepository, ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(AddRepairNoteCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        TicketAccessGuard.EnsureCanAccess(ticket, _currentUser);

        ticket.AddNote(request.Note); // Task 4.1 — không đổi trạng thái, không cần TrackNewX
        await _ticketRepository.SaveChangesAsync();
        return Unit.Value;
    }
}