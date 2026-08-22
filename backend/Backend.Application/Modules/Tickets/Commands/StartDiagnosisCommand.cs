using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Tickets.DTOs;
using RepairShop.Domain.Common;

namespace RepairShop.Application.Modules.Tickets.Commands;

public record StartDiagnosisCommand(Guid TicketId) : IRequest<TicketResponse>;

public class StartDiagnosisCommandHandler : IRequestHandler<StartDiagnosisCommand, TicketResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly IRepairStatusRepository _statusRepository;
    private readonly ICurrentUserService _currentUser;

    public StartDiagnosisCommandHandler(IRepairTicketRepository ticketRepository,
        IRepairStatusRepository statusRepository, ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _statusRepository = statusRepository;
        _currentUser = currentUser;
    }

    public async Task<TicketResponse> Handle(StartDiagnosisCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        RepairShop.Application.Common.Authorization.TicketAccessGuard.EnsureCanAccess(ticket, _currentUser);

        var diagnosingStatus = await _statusRepository.GetByCodeAsync(RepairStatusCodes.Diagnosing);
        var userId = _currentUser.UserId!.Value;

        ticket.StartDiagnosis(diagnosingStatus, userId); // Task 4.2 — tự enforce transition ASSIGNED→DIAGNOSING

        // Ticket đã tracked Unchanged (load qua GetByIdAsync) → StatusHistory mới cần Track tường minh
        _ticketRepository.TrackNewStatusHistory(ticket.StatusHistories.Last());

        await _ticketRepository.SaveChangesAsync();

        return MapToResponse(ticket);
    }

    private static TicketResponse MapToResponse(RepairShop.Domain.Modules.Tickets.RepairTicket ticket) =>
        new(ticket.Id, ticket.TicketCode, ticket.CustomerId, ticket.DeviceId, ticket.Status.Code,
            ticket.IssueReported, ticket.Notes, ticket.ConditionNotes, ticket.RiskWarning, ticket.ReceivedAt);
}