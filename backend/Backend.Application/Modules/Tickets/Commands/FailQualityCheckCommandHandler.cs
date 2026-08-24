using MediatR;
using Microsoft.Extensions.Logging;
using RepairShop.Application.Common.Authorization;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Tickets;
using RepairShop.Application.Modules.Tickets.DTOs;
using RepairShop.Domain.Common;

public class FailQualityCheckCommandHandler : IRequestHandler<FailQualityCheckCommand, TicketResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly IRepairStatusRepository _statusRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<FailQualityCheckCommandHandler> _logger;

    public FailQualityCheckCommandHandler(IRepairTicketRepository ticketRepository,
        IRepairStatusRepository statusRepository, ICurrentUserService currentUser,
        ILogger<FailQualityCheckCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _statusRepository = statusRepository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<TicketResponse> Handle(FailQualityCheckCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        TicketAccessGuard.EnsureCanAccess(ticket, _currentUser);

        var userId = _currentUser.UserId!.Value;
        var inRepairStatus = await _statusRepository.GetByCodeAsync(RepairStatusCodes.InRepair);

        // Ticket quay lại IN_REPAIR -> CompletionNotes CŨ vẫn còn giá trị cũ (không tự xoá) — Technician
        // cần RecordCompletionNotes() lại lần nữa trước khi StartQualityCheck() lần 2 (do gate ở Task 4.10
        // check string.IsNullOrWhiteSpace, còn giá trị cũ thì KHÔNG null -> lưu ý này cần ghi chú thêm bên dưới).
        ticket.FailQualityCheck(inRepairStatus, userId, request.FailureReason);

        _ticketRepository.TrackNewStatusHistory(ticket.StatusHistories.Last());
        await _ticketRepository.SaveChangesAsync();

        _logger.LogWarning("Ticket {TicketCode} QA KHÔNG ĐẠT: {Reason}, quay lại IN_REPAIR",
            ticket.TicketCode, request.FailureReason);

        return TicketMapper.ToResponse(ticket);
    }
}