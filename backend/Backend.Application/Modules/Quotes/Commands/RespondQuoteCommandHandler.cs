using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Common;

public class RespondQuoteCommandHandler : IRequestHandler<RespondQuoteCommand, QuoteResponse>
{
    private readonly IQuoteRepository _quoteRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IRepairStatusRepository _statusRepository;
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly ICurrentUserService _currentUser;

    public RespondQuoteCommandHandler(IQuoteRepository quoteRepository, ICustomerRepository customerRepository,
        IRepairStatusRepository statusRepository, IRepairTicketRepository ticketRepository,
        ICurrentUserService currentUser)
    {
        _quoteRepository = quoteRepository;
        _customerRepository = customerRepository;
        _statusRepository = statusRepository;
        _ticketRepository = ticketRepository;
        _currentUser = currentUser;
    }

    public async Task<QuoteResponse> Handle(RespondQuoteCommand request, CancellationToken cancellationToken)
    {
        var quote = await _quoteRepository.GetByIdAsync(request.QuoteId)
            ?? throw new NotFoundException("Báo giá", request.QuoteId);

        // Ownership: Customer chỉ phản hồi được quote của chính mình (khác Role Authorization ở Controller)
        var customer = await _customerRepository.GetByUserIdAsync(_currentUser.UserId!.Value);
        RepairShop.Application.Common.Authorization.QuoteAccessGuard.EnsureCustomerOwnsQuote(quote, customer);

        var ticket = quote.RepairTicket;
        var userId = _currentUser.UserId!.Value;

        if (request.Decision.Equals("APPROVED", StringComparison.OrdinalIgnoreCase))
        {
            quote.Approve(); // Task 4.1 — tự chặn nếu Quote không còn Pending

            // Mặc định chuyển IN_REPAIR (giả định đủ tồn kho lúc này — kiểm tra tồn kho thật
            // thuộc phạm vi UsePart()/Task 4.2, sẽ nối vào khi làm nghiệp vụ ghi nhận linh kiện)
            var inRepairStatus = await _statusRepository.GetByCodeAsync(RepairStatusCodes.InRepair);
            ticket.ApproveQuote(inRepairStatus, userId);
        }
        else if (request.Decision.Equals("REJECTED", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.RejectReason))
                throw new RepairShop.Domain.Common.Exceptions.DomainException("Phải nêu lý do khi từ chối báo giá.");

            quote.Reject(request.RejectReason);

            var closedStatus = await _statusRepository.GetByCodeAsync(RepairStatusCodes.ClosedRejected);
            ticket.RejectQuote(closedStatus, userId, request.RejectReason);
        }
        else
        {
            throw new RepairShop.Domain.Common.Exceptions.DomainException(
                "Decision phải là 'APPROVED' hoặc 'REJECTED'.");
        }

        _ticketRepository.TrackNewStatusHistory(ticket.StatusHistories.Last()); // ticket tracked Unchanged qua navigation quote.RepairTicket

        await _quoteRepository.SaveChangesAsync();

        return new QuoteResponse(quote.Id, quote.RepairTicketId, quote.Description, quote.TotalAmount,
            quote.Status.ToString(),
            quote.Items.Select(i => new QuoteItemResponse(i.Id, i.ItemType.ToString(), i.Description,
                i.Quantity, i.UnitPrice, i.Subtotal)).ToList(),
            quote.CreatedAt);
    }
}