using MediatR;
using Microsoft.Extensions.Logging;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Common;
using RepairShop.Domain.Modules.Quotes;
using RepairShop.Domain.Modules.Quotes.Enums;

public class CreateQuoteCommandHandler : IRequestHandler<CreateQuoteCommand, QuoteResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly IQuoteRepository _quoteRepository;
    private readonly IRepairStatusRepository _statusRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CreateQuoteCommandHandler> _logger;

    public CreateQuoteCommandHandler(IRepairTicketRepository ticketRepository, IQuoteRepository quoteRepository,
        IRepairStatusRepository statusRepository, ICurrentUserService currentUser,
        ILogger<CreateQuoteCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _quoteRepository = quoteRepository;
        _statusRepository = statusRepository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<QuoteResponse> Handle(CreateQuoteCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        var userId = _currentUser.UserId!.Value;

        // FR-030: dựa trên kết quả chẩn đoán — Domain (SubmitQuote) đã tự kiểm tra DiagnosisResult != null,
        // và tự chặn nếu ticket không ở DIAGNOSING (transition rule, Task 4.2)
        var quote = new Quote(ticket.Id, request.Description, userId);

        foreach (var item in request.Items)
        {
            var itemType = Enum.Parse<QuoteItemType>(item.ItemType, ignoreCase: true);
            quote.AddItem(itemType, item.Description, item.Quantity, item.UnitPrice, item.PartId);
        }

        ticket.AttachQuote(quote); // giữ đúng quan hệ domain (Task 4.1)

        var waitingApprovalStatus = await _statusRepository.GetByCodeAsync(RepairStatusCodes.WaitingApproval);
        ticket.SubmitQuote(waitingApprovalStatus, userId); // DIAGNOSING -> WAITING_APPROVAL

        // Quote là entity MỚI hoàn toàn -> Add tường minh, EF tự cascade-Added QuoteItems bên trong
        await _quoteRepository.AddAsync(quote);

        // ticket đã tracked Unchanged (GetByIdAsync) -> StatusHistory mới cần Track tường minh (như Task 4.6)
        _ticketRepository.TrackNewStatusHistory(ticket.StatusHistories.Last());

        await _quoteRepository.SaveChangesAsync(); // dùng chung 1 DbContext, SaveChanges 1 lần là đủ cho cả 2 thay đổi

        _logger.LogInformation("Tạo Quote {QuoteId} cho Ticket {TicketCode}, tổng tiền {TotalAmount}",
            quote.Id, ticket.TicketCode, quote.TotalAmount);

        return MapToResponse(quote);
    }

    private static QuoteResponse MapToResponse(Quote quote) => new(
        quote.Id, quote.RepairTicketId, quote.Description, quote.TotalAmount, quote.Status.ToString(),
        quote.Items.Select(i => new QuoteItemResponse(i.Id, i.ItemType.ToString(), i.Description,
            i.Quantity, i.UnitPrice, i.Subtotal)).ToList(),
        quote.CreatedAt);
}