using Backend.Domain.Common;
using Backend.Domain.Common.Enums;
using Backend.Domain.Modules.Identity;
using Backend.Domain.Modules.Tickets;
using Backend.Domain.Common.Exceptions;


namespace Backend.Domain.Modules.Quotes;

public class Quote : BaseEntity
{
    public Guid RepairTicketId { get; private set; }
    public string Description { get; private set; } = default!;
    public decimal TotalAmount { get; private set; }
    public QuoteStatus Status { get; private set; } = QuoteStatus.Pending;
    public Guid CreatedByUserId { get; private set; }
    public string? RejectReason { get; private set; }
    public DateTime? RespondedAt { get; private set; }

    public RepairTicket RepairTicket { get; private set; } = default!;
    public User CreatedByUser { get; private set; } = default!;

    private Quote() { } // for EF Core

    public Quote(Guid repairTicketId, string description, decimal totalAmount, Guid createdByUserId)
    {
        if (totalAmount < 0)
            throw new DomainException("Tổng tiền báo giá không thể âm.");

        RepairTicketId = repairTicketId;
        Description = description;
        TotalAmount = totalAmount;
        CreatedByUserId = createdByUserId;
    }

    public void Approve()
    {
        if (Status != QuoteStatus.Pending)
            throw new DomainException(
                "Chỉ có thể duyệt báo giá đang ở trạng thái Pending.");

        Status = QuoteStatus.Approved;
        RespondedAt = DateTime.UtcNow;
        MarkUpdated();
    }

    public void Reject(string reason)
    {
        if (Status != QuoteStatus.Pending)
            throw new DomainException(
                "Chỉ có thể từ chối báo giá đang ở trạng thái Pending.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException(
                "Lý do từ chối báo giá không được để trống.");
                
        Status = QuoteStatus.Rejected;
        RejectReason = reason;
        RespondedAt = DateTime.UtcNow;
        MarkUpdated();
    }
}