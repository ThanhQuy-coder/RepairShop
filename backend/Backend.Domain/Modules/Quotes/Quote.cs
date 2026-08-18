using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Common.Exceptions;
using RepairShop.Domain.Modules.Identity;
using RepairShop.Domain.Modules.Tickets;

namespace RepairShop.Domain.Modules.Quotes;

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

    private readonly List<QuoteItem> _items = new();
    public IReadOnlyCollection<QuoteItem> Items => _items.AsReadOnly();

    private Quote() { } // for EF Core

    public Quote(Guid repairTicketId, string description, Guid createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Mô tả báo giá không được để trống.");

        RepairTicketId = repairTicketId;
        Description = description;
        CreatedByUserId = createdByUserId;
    }

    /// <summary>Thêm hạng mục — tự cộng dồn vào TotalAmount, chỉ cho phép khi Quote còn Pending.</summary>
    public void AddItem(string description, int quantity, decimal unitPrice)
    {
        if (Status != QuoteStatus.Pending)
            throw new DomainException("Không thể chỉnh sửa báo giá đã được khách phản hồi.");

        var item = new QuoteItem(Id, description, quantity, unitPrice);
        _items.Add(item);
        TotalAmount = _items.Sum(i => i.Subtotal);
    }

    public void Approve()
    {
        if (Status != QuoteStatus.Pending)
            throw new DomainException("Chỉ báo giá đang chờ (Pending) mới có thể được duyệt.");
        if (_items.Count == 0)
            throw new DomainException("Không thể duyệt báo giá chưa có hạng mục nào.");

        Status = QuoteStatus.Approved;
        RespondedAt = DateTime.UtcNow;
        MarkUpdated();
    }

    public void Reject(string reason)
    {
        if (Status != QuoteStatus.Pending)
            throw new DomainException("Chỉ báo giá đang chờ (Pending) mới có thể bị từ chối.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Phải nêu lý do khi từ chối báo giá.");

        Status = QuoteStatus.Rejected;
        RejectReason = reason;
        RespondedAt = DateTime.UtcNow;
        MarkUpdated();
    }
}