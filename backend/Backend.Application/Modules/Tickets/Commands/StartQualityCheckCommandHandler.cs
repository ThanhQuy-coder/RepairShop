using MediatR;
using RepairShop.Application.Common.Authorization;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Tickets;
using RepairShop.Application.Modules.Tickets.DTOs;
using RepairShop.Domain.Common;

public class StartQualityCheckCommandHandler : IRequestHandler<StartQualityCheckCommand, TicketResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly IRepairStatusRepository _statusRepository;
    private readonly ICurrentUserService _currentUser;

    public StartQualityCheckCommandHandler(IRepairTicketRepository ticketRepository,
        IRepairStatusRepository statusRepository, ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _statusRepository = statusRepository;
        _currentUser = currentUser;
    }

    public async Task<TicketResponse> Handle(StartQualityCheckCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        TicketAccessGuard.EnsureCanAccess(ticket, _currentUser);

        var qaStatus = await _statusRepository.GetByCodeAsync(RepairStatusCodes.QaTesting);
        var userId = _currentUser.UserId!.Value;

        // Domain tự chặn nếu chưa có CompletionNotes (Task 4.10) hoặc sai trạng thái (Task 4.2)
        ticket.StartQualityCheck(qaStatus, userId);

        _ticketRepository.TrackNewStatusHistory(ticket.StatusHistories.Last());
        await _ticketRepository.SaveChangesAsync();

        return TicketMapper.ToResponse(ticket);
    }
}