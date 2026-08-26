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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApproveQuoteCommandHandler> _logger;

    public ApproveQuoteCommandHandler(IQuoteRepository quoteRepository, ICustomerRepository customerRepository,
        IRepairTicketRepository ticketRepository, IRepairStatusRepository statusRepository,
        ICurrentUserService currentUser, IUnitOfWork unitOfWork, ILogger<ApproveQuoteCommandHandler> logger)
    {
        _quoteRepository = quoteRepository;
        _customerRepository = customerRepository;
        _ticketRepository = ticketRepository;
        _statusRepository = statusRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
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

        // Bọc TOÀN BỘ 3 thao tác (Approve Quote + Update Ticket Status + tạo Status History) trong
        // 1 transaction — đúng ví dụ mentor: nếu bước tạo Status History fail (VD lỗi DB tạm thời),
        // toàn bộ rollback, KHÔNG để xảy ra Quote=APPROVED nhưng Ticket vẫn WAITING_APPROVAL.
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            quote.Approve();

            var inRepairStatus = await _statusRepository.GetByCodeAsync(RepairStatusCodes.InRepair);
            ticket.ApproveQuote(inRepairStatus, userId);

            _ticketRepository.TrackNewStatusHistory(ticket.StatusHistories.Last());

            await _quoteRepository.SaveChangesAsync();
        }, cancellationToken);

        _logger.LogInformation("Quote {QuoteId} được duyệt, Ticket {TicketCode} chuyển IN_REPAIR",
            quote.Id, ticket.TicketCode);

        return QuoteMapper.ToResponse(quote);
    }
}