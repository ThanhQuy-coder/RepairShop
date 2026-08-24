using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Common.Exceptions;
using RepairShop.Domain.Modules.Billing.Enums;

public class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, InvoiceResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly ICurrentUserService _currentUser;

    public CreateInvoiceCommandHandler(IRepairTicketRepository ticketRepository, ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _currentUser = currentUser;
    }

    public async Task<InvoiceResponse> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        // Lấy tổng tiền từ Quote đã APPROVED — không cho Receptionist tự gõ tay số tiền tùy ý,
        // tránh hóa đơn lệch khỏi báo giá khách đã đồng ý.
        var approvedQuote = ticket.Quotes.FirstOrDefault(q => q.Status == QuoteStatus.Approved)
            ?? throw new DomainException("Ticket chưa có báo giá được duyệt, không thể xuất hóa đơn.");

        var paymentMethod = Enum.Parse<PaymentMethod>(request.PaymentMethod, ignoreCase: true);
        var userId = _currentUser.UserId!.Value;

        var invoice = ticket.CreateInvoice(paymentMethod, approvedQuote.TotalAmount, userId, approvedQuote.Id);
        _ticketRepository.TrackNewInvoice(invoice);

        await _ticketRepository.SaveChangesAsync();

        return new InvoiceResponse(invoice.Id, ticket.Id, invoice.TotalAmount,
            invoice.PaymentMethod.ToString(), invoice.PaidAt, invoice.CreatedAt);
    }
}