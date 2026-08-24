using MediatR;
using RepairShop.Application.Common.Exceptions;

public class MarkInvoicePaidCommandHandler : IRequestHandler<MarkInvoicePaidCommand, InvoiceResponse>
{
    private readonly IInvoiceRepository _invoiceRepository;

    public MarkInvoicePaidCommandHandler(IInvoiceRepository invoiceRepository) => _invoiceRepository = invoiceRepository;

    public async Task<InvoiceResponse> Handle(MarkInvoicePaidCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId)
            ?? throw new NotFoundException("Hóa đơn", request.InvoiceId);

        invoice.MarkAsPaid(request.PaidAt); // Domain tự chặn nếu đã thanh toán trước đó
        await _invoiceRepository.SaveChangesAsync();

        return new InvoiceResponse(invoice.Id, invoice.RepairTicketId, invoice.TotalAmount,
            invoice.PaymentMethod.ToString(), invoice.PaidAt, invoice.CreatedAt);
    }
}