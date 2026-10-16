using MediatR;
using Microsoft.Extensions.Logging;
using RepairShop.Application.Common.Authorization;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Quotes;

public class RejectQuoteCommandHandler : IRequestHandler<RejectQuoteCommand, QuoteResponse>
{
    private readonly IQuoteRepository _quoteRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly IRepairStatusRepository _statusRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RejectQuoteCommandHandler> _logger;

    public RejectQuoteCommandHandler(IQuoteRepository quoteRepository, ICustomerRepository customerRepository,
        IRepairTicketRepository ticketRepository, IRepairStatusRepository statusRepository,
        ICurrentUserService currentUser, ILogger<RejectQuoteCommandHandler> logger, IUnitOfWork unitOfWork)
    {
        _quoteRepository = quoteRepository;
        _customerRepository = customerRepository;
        _ticketRepository = ticketRepository;
        _statusRepository = statusRepository;
        _currentUser = currentUser;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<QuoteResponse> Handle(RejectQuoteCommand request, CancellationToken cancellationToken)
    {
        var quote = await _quoteRepository.GetByIdAsync(request.QuoteId)
            ?? throw new NotFoundException("Báo giá", request.QuoteId);

        var customer = await _customerRepository.GetByUserIdAsync(_currentUser.UserId!.Value);
        QuoteAccessGuard.EnsureCustomerOwnsQuote(quote, customer);

        var userId = _currentUser.UserId!.Value;
        var ticket = quote.RepairTicket;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            quote.Reject(request.RejectReason);

            await _quoteRepository.SaveChangesAsync();
        }, cancellationToken);
        
        _logger.LogInformation("Quote {QuoteId} bị từ chối, Ticket {TicketCode} sẵn sàng cho NEEDS_REQUOTE",
            quote.Id, ticket.TicketCode);

        return QuoteMapper.ToResponse(quote);
    }
}