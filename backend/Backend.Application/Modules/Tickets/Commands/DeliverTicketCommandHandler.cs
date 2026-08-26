using MediatR;
using Microsoft.Extensions.Logging;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Tickets;
using RepairShop.Application.Modules.Tickets.DTOs;
using RepairShop.Domain.Common;

public class DeliverTicketCommandHandler : IRequestHandler<DeliverTicketCommand, TicketResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly IRepairStatusRepository _statusRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeliverTicketCommandHandler> _logger;

    public DeliverTicketCommandHandler(IRepairTicketRepository ticketRepository,
        IRepairStatusRepository statusRepository, ICurrentUserService currentUser,
        ILogger<DeliverTicketCommandHandler> logger,
        IUnitOfWork unitOfWork)
    {
        _ticketRepository = ticketRepository;
        _statusRepository = statusRepository;
        _currentUser = currentUser;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<TicketResponse> Handle(DeliverTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        var deliveredStatus = await _statusRepository.GetByCodeAsync(RepairStatusCodes.Delivered);
        var userId = _currentUser.UserId!.Value;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            ticket.Deliver(deliveredStatus, userId, request.DeliveryNote); // enforce đã thanh toán (Task 4.12)
            _ticketRepository.TrackNewStatusHistory(ticket.StatusHistories.Last());
            await _ticketRepository.SaveChangesAsync();
        }, cancellationToken);

        _logger.LogInformation("Ticket {TicketCode} đã bàn giao cho khách", ticket.TicketCode);
        return TicketMapper.ToResponse(ticket);
    }
}