using RepairShop.Application.Common.Authorization;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace RepairShop.Application.Modules.Quotes.Commands;

public class ApproveQuoteCommandHandler : IRequestHandler<ApproveQuoteCommand, QuoteResponse>
{
    private readonly IQuoteRepository _quoteRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly IRepairStatusRepository _statusRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ApproveQuoteCommandHandler> _logger;

    public ApproveQuoteCommandHandler(IQuoteRepository quoteRepository, ICustomerRepository customerRepository,
        IRepairTicketRepository ticketRepository, IRepairStatusRepository statusRepository,
        ICurrentUserService currentUser, ILogger<ApproveQuoteCommandHandler> logger)
    {
        _quoteRepository = quoteRepository;
        _customerRepository = customerRepository;
        _ticketRepository = ticketRepository;
        _statusRepository = statusRepository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<QuoteResponse> Handle(ApproveQuoteCommand request, CancellationToken cancellationToken)
    {
        var quote = await _quoteRepository.GetByIdAsync(request.QuoteId)
            ?? throw new NotFoundException("Báo giá", request.QuoteId);

        var customer = await _customerRepository.GetByUserIdAsync(_currentUser.UserId!.Value);
        QuoteAccessGuard.EnsureCustomerOwnsQuote(quote, customer);

        var userId = _currentUser.UserId!.Value;
        var ticket = quote.RepairTicket;

        // Rule mentor: "Nếu Quote = APPROVED thì mới cho phép tiếp tục sửa"
        // -> Approve() chỉ đổi status Quote, KHÔNG tự quyết định ticket đi đâu.
        // Ticket chuyển IN_REPAIR là hệ quả TƯỜNG MINH ngay sau đó, không giấu logic bên trong Quote.
        quote.Approve();

        var inRepairStatus = await _statusRepository.GetByCodeAsync(RepairStatusCodes.InRepair);
        ticket.ApproveQuote(inRepairStatus, userId);

        _ticketRepository.TrackNewStatusHistory(ticket.StatusHistories.Last());

        await _quoteRepository.SaveChangesAsync();

        _logger.LogInformation("Quote {QuoteId} được duyệt, Ticket {TicketCode} chuyển IN_REPAIR",
            quote.Id, ticket.TicketCode);

        return QuoteMapper.ToResponse(quote);
    }
}