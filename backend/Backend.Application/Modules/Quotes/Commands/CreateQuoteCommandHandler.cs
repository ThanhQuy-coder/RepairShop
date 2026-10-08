using MediatR;
using Microsoft.Extensions.Logging;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Quotes;
using RepairShop.Application.Modules.Tickets;
using RepairShop.Domain.Common;
using RepairShop.Domain.Modules.Quotes;
using RepairShop.Domain.Modules.Quotes.Enums;

public class CreateQuoteCommandHandler : IRequestHandler<CreateQuoteCommand, QuoteResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly IQuoteRepository _quoteRepository;
    private readonly IRepairStatusRepository _statusRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ICustomerRepository _customerRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CreateQuoteCommandHandler> _logger;

    public CreateQuoteCommandHandler(IRepairTicketRepository ticketRepository,
    IQuoteRepository quoteRepository,
        IRepairStatusRepository statusRepository,
        ICurrentUserService currentUser,
        ICustomerRepository customerRepository,
        INotificationService notificationService,
        ILogger<CreateQuoteCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _quoteRepository = quoteRepository;
        _statusRepository = statusRepository;
        _currentUser = currentUser;
        _customerRepository = customerRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<QuoteResponse> Handle(CreateQuoteCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        var userId = _currentUser.UserId!.Value;

        var previous = ticket.Quotes.OrderByDescending(q => q.Version).FirstOrDefault();
        var isRequote = previous?.Status is RepairShop.Domain.Common.Enums.QuoteStatus.QuoteRejected
            or RepairShop.Domain.Common.Enums.QuoteStatus.NeedsRequote;
        if (isRequote)
            previous!.MarkNeedsRequote();

        var quote = new Quote(ticket.Id, request.Description, userId,
            (previous?.Version ?? 0) + 1, previous?.Id);

        foreach (var item in request.Items)
        {
            var itemType = Enum.Parse<QuoteItemType>(item.ItemType, ignoreCase: true);
            quote.AddItem(itemType, item.Description, item.Quantity, item.UnitPrice, item.PartId);
        }

        ticket.AttachQuote(quote); // giữ đúng quan hệ domain (Task 4.1)

        var waitingApprovalStatus = await _statusRepository.GetByCodeAsync(RepairStatusCodes.WaitingApproval);
        if (!isRequote)
            ticket.SubmitQuote(waitingApprovalStatus, userId); // DIAGNOSING -> WAITING_APPROVAL

        // Quote là entity MỚI hoàn toàn -> Add tường minh, EF tự cascade-Added QuoteItems bên trong
        await _quoteRepository.AddAsync(quote);

        // ticket đã tracked Unchanged (GetByIdAsync) -> StatusHistory mới cần Track tường minh (như Task 4.6)
        _ticketRepository.TrackNewStatusHistory(ticket.StatusHistories.Last());

        await _quoteRepository.SaveChangesAsync(); // dùng chung 1 DbContext, SaveChanges 1 lần là đủ cho cả 2 thay đổi

        await TicketNotificationHelper.NotifyCustomerIfLinkedAsync(
            _customerRepository, _notificationService, ticket.CustomerId,
            "QuoteCreated", "Báo giá mới cần xác nhận",
            $"Phiếu {ticket.TicketCode} đã có báo giá {quote.TotalAmount:N0}đ, vui lòng xem và phản hồi.",
            ticket.Id);

        _logger.LogInformation("Tạo Quote {QuoteId} cho Ticket {TicketCode}, tổng tiền {TotalAmount}",
            quote.Id, ticket.TicketCode, quote.TotalAmount);

        return QuoteMapper.ToResponse(quote);
    }
}