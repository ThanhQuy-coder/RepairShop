using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Exceptions;
using RepairShop.Domain.Modules.Billing.Enums;

namespace RepairShop.Domain.Modules.Billing;

public class Invoice : BaseEntity
{
    public Guid RepairTicketId { get; private set; }
    public Guid? QuoteId { get; private set; }
    public decimal TotalAmount { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private Invoice() { } // for EF Core

    internal Invoice(Guid repairTicketId, Guid? quoteId, decimal totalAmount,
        PaymentMethod paymentMethod, Guid createdByUserId)
    {
        if (totalAmount < 0)
            throw new DomainException("Tổng tiền hóa đơn không thể âm.");

        RepairTicketId = repairTicketId;
        QuoteId = quoteId;
        TotalAmount = totalAmount;
        PaymentMethod = paymentMethod;
        CreatedByUserId = createdByUserId;
    }

    /// <summary>
    /// "Payment = Mock/Manual" (mentor): không gọi cổng thanh toán thật, chỉ ghi nhận thủ công
    /// thời điểm Receptionist xác nhận đã nhận tiền (tiền mặt/chuyển khoản).
    /// </summary>
    public void MarkAsPaid(DateTime? paidAt = null)
    {
        if (PaidAt is not null)
            throw new DomainException("Hóa đơn này đã được thanh toán trước đó.");

        PaidAt = paidAt ?? DateTime.UtcNow;
    }
}