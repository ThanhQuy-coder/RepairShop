using MediatR;
using Microsoft.Extensions.Logging;
using RepairShop.Application.Common.Authorization;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Quotes;
using RepairShop.Domain.Common;

public class RejectQuoteCommandHandler : IRequestHandler<RejectQuoteCommand, QuoteResponse>
{
    private readonly IQuoteRepository _quoteRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly IRepairStatusRepository _statusRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<RejectQuoteCommandHandler> _logger;

    public RejectQuoteCommandHandler(IQuoteRepository quoteRepository, ICustomerRepository customerRepository,
        IRepairTicketRepository ticketRepository, IRepairStatusRepository statusRepository,
        ICurrentUserService currentUser, ILogger<RejectQuoteCommandHandler> logger)
    {
        _quoteRepository = quoteRepository;
        _customerRepository = customerRepository;
        _ticketRepository = ticketRepository;
        _statusRepository = statusRepository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<QuoteResponse> Handle(RejectQuoteCommand request, CancellationToken cancellationToken)
    {
        var quote = await _quoteRepository.GetByIdAsync(request.QuoteId)
            ?? throw new NotFoundException("Báo giá", request.QuoteId);

        var customer = await _customerRepository.GetByUserIdAsync(_currentUser.UserId!.Value);
        QuoteAccessGuard.EnsureCustomerOwnsQuote(quote, customer);

        var userId = _currentUser.UserId!.Value;
        var ticket = quote.RepairTicket;

        // Rule mentor: "Nếu Quote = REJECTED thì Ticket phải đi theo nhánh xử lý từ chối"
        // -> KHÔNG có đường nào khác ngoài CLOSED_REJECTED. RejectQuote() trong RepairTicketStateMachine
        //    (Task 4.2) chỉ cho WAITING_APPROVAL -> CLOSED_REJECTED, không có lựa chọn thứ 2 -> đúng
        //    tinh thần "theo state machine đã chốt", không phải nhánh tự do CANCELLED/CLOSED tùy chọn.
        quote.Reject(request.RejectReason);

        var closedStatus = await _statusRepository.GetByCodeAsync(RepairStatusCodes.ClosedRejected);
        ticket.RejectQuote(closedStatus, userId, request.RejectReason);

        _ticketRepository.TrackNewStatusHistory(ticket.StatusHistories.Last());

        await _quoteRepository.SaveChangesAsync();

        _logger.LogInformation("Quote {QuoteId} bị từ chối, Ticket {TicketCode} chuyển CLOSED_REJECTED",
            quote.Id, ticket.TicketCode);

        return QuoteMapper.ToResponse(quote);
    }
}