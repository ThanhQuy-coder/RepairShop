using RepairShop.Domain.Common;
using RepairShop.Domain.Modules.Tickets;
using RepairShop.Domain.Common.Exceptions;
using RepairShop.Domain.Modules.Warranty.Enums;

namespace RepairShop.Domain.Modules.Warranty;

public class Warranty : BaseEntity
{
    public string WarrantyCode { get; private set; } = default!;
    public Guid RepairTicketId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public string? Terms { get; private set; }
    public WarrantyStatus Status { get; private set; } = WarrantyStatus.Active;

    public RepairTicket RepairTicket { get; private set; } = default!;

    private Warranty() { } // for EF Core

    public Warranty(string warrantyCode, Guid repairTicketId,
        DateOnly startDate, DateOnly endDate, string? terms = null)
    {
        if (string.IsNullOrWhiteSpace(warrantyCode))
            throw new DomainException("Mã bảo hành không được để trống.");
        if (endDate <= startDate) // validate đã đề xuất từ trước, giờ hiện thực hoá
            throw new DomainException("Ngày kết thúc bảo hành phải sau ngày bắt đầu.");

        WarrantyCode = warrantyCode;
        RepairTicketId = repairTicketId;
        StartDate = startDate;
        EndDate = endDate;
        Terms = terms;
    }

    public bool IsExpired() => DateOnly.FromDateTime(DateTime.UtcNow) > EndDate;

    public void Void(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Phải nêu lý do khi huỷ bảo hành.");

        Status = WarrantyStatus.Voided;
        MarkUpdated();
    }
}