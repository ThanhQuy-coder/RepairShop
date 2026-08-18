using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Exceptions;
using RepairShop.Domain.Modules.Customers;
using RepairShop.Domain.Modules.Devices;
using RepairShop.Domain.Modules.Identity;
using RepairShop.Domain.Modules.Inventory;
using RepairShop.Domain.Modules.Quotes;
using RepairShop.Domain.Modules.Tickets.Enums;

namespace RepairShop.Domain.Modules.Tickets;

public class RepairTicket : BaseEntity
{
    public string TicketCode { get; private set; } = default!;
    public Guid CustomerId { get; private set; }
    public Guid DeviceId { get; private set; }
    public Guid ReceptionistId { get; private set; }
    public Guid? TechnicianId { get; private set; }
    public int StatusId { get; private set; }
    public string IssueReported { get; private set; } = default!;
    public string? DiagnosisResult { get; private set; }
    public string? Notes { get; private set; }
    public decimal DiagnosticDeposit { get; private set; }
    public Guid? ParentTicketId { get; private set; }

    public DateTime ReceivedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }

    public Customer Customer { get; private set; } = default!;
    public Device Device { get; private set; } = default!;
    public User Receptionist { get; private set; } = default!;
    public User? Technician { get; private set; }
    public RepairStatus Status { get; private set; } = default!;
    public RepairTicket? ParentTicket { get; private set; }
    public ICollection<RepairTicket> WarrantyTickets { get; private set; } = new List<RepairTicket>();
    public Warranty.Warranty? Warranty { get; private set; }

    private readonly List<Quote> _quotes = new();
    public IReadOnlyCollection<Quote> Quotes => _quotes.AsReadOnly();

    private readonly List<TicketImage> _images = new();
    public IReadOnlyCollection<TicketImage> Images => _images.AsReadOnly();

    private readonly List<RepairTicketStatusHistory> _statusHistories = new();
    public IReadOnlyCollection<RepairTicketStatusHistory> StatusHistories => _statusHistories.AsReadOnly();

    private readonly List<TicketPart> _ticketParts = new();
    public IReadOnlyCollection<TicketPart> TicketParts => _ticketParts.AsReadOnly();

    private RepairTicket() { } // for EF Core

    public RepairTicket(string ticketCode, Guid customerId, Guid deviceId, Guid receptionistId,
        string issueReported, RepairStatus checkedInStatus, decimal diagnosticDeposit = 0,
        Guid? parentTicketId = null)
    {
        if (string.IsNullOrWhiteSpace(ticketCode))
            throw new DomainException("TicketCode không được để trống.");
        if (string.IsNullOrWhiteSpace(issueReported))
            throw new DomainException("Mô tả lỗi (IssueReported) không được để trống.");
        if (checkedInStatus.Code != RepairStatusCodes.CheckedIn)
            throw new DomainException("Ticket mới tạo bắt buộc phải ở trạng thái CHECKED_IN.");
        if (diagnosticDeposit < 0)
            throw new DomainException("Tiền cọc chẩn đoán không thể âm.");
        if (parentTicketId == null && diagnosticDeposit < 0)
            throw new DomainException("Tiền cọc không hợp lệ.");

        TicketCode = ticketCode;
        CustomerId = customerId;
        DeviceId = deviceId;
        ReceptionistId = receptionistId;
        IssueReported = issueReported;
        DiagnosticDeposit = diagnosticDeposit;
        ParentTicketId = parentTicketId;
        ReceivedAt = DateTime.UtcNow;

        StatusId = checkedInStatus.Id;
        Status = checkedInStatus;
    }

    // ───────────────────────── Ghi nhận ảnh hiện trạng ─────────────────────────

    /// <summary>FR-017/FR-026: Receptionist chụp ảnh lúc nhận, Technician chụp ảnh sau sửa.</summary>
    public void AddImage(string imageUrl, ImageType imageType)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new DomainException("URL ảnh không được để trống.");

        _images.Add(new TicketImage(Id, imageUrl, imageType));
    }

    // ───────────────────────── Workflow: Assign → Diagnosis ─────────────────────────

    public void AssignTechnician(Guid technicianId, RepairStatus assignedStatus, Guid changedByUserId, string? note = null)
    {
        TechnicianId = technicianId;
        ChangeStatus(assignedStatus, changedByUserId, note);
    }

    public void StartDiagnosis(RepairStatus diagnosingStatus, Guid changedByUserId, string? note = null)
    {
        EnsureTechnicianAssigned();

        ChangeStatus(diagnosingStatus, changedByUserId, note);
    }

    /// <summary>FR-024: chỉ ghi được kết quả chẩn đoán khi đang ở bước Diagnosing.</summary>
    public void SubmitDiagnosis(string diagnosisResult)
    {
        if (Status.Code != RepairStatusCodes.Diagnosing)
            throw new DomainException($"Chỉ ghi nhận chẩn đoán khi ticket ở trạng thái DIAGNOSING (hiện tại: '{Status.Code}').");
        if (string.IsNullOrWhiteSpace(diagnosisResult))
            throw new DomainException("Kết quả chẩn đoán không được để trống.");

        DiagnosisResult = diagnosisResult;
        MarkUpdated();
    }

    public void AddNote(string note)
    {
        if (string.IsNullOrWhiteSpace(note)) return;
        Notes = string.IsNullOrWhiteSpace(Notes) ? note : $"{Notes}\n{note}";
        MarkUpdated();
    }

    // ───────────────────────── Workflow: Quote ─────────────────────────

    public void SubmitQuote(RepairStatus waitingApprovalStatus, Guid changedByUserId, string? note = null)
    {
        if (string.IsNullOrWhiteSpace(DiagnosisResult))
            throw new DomainException("Phải có kết quả chẩn đoán trước khi gửi báo giá cho khách.");

        ChangeStatus(waitingApprovalStatus, changedByUserId, note);
    }

    public void AttachQuote(Quote quote)
    {
        if (quote.RepairTicketId != Id)
            throw new DomainException("Quote không thuộc về ticket này.");
        _quotes.Add(quote);
    }

    public void ApproveQuote(RepairStatus nextStatus, Guid changedByUserId, string? note = null)
    {
        if (nextStatus.Code is not (RepairStatusCodes.InRepair or RepairStatusCodes.WaitingParts))
            throw new DomainException("Sau khi duyệt báo giá chỉ có thể chuyển sang IN_REPAIR hoặc WAITING_PARTS.");

        ChangeStatus(nextStatus, changedByUserId, note);
    }

    public void RejectQuote(RepairStatus closedStatus, Guid changedByUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Phải nêu lý do khi từ chối báo giá.");

        ChangeStatus(closedStatus, changedByUserId, reason);
    }

    // ───────────────────────── Workflow: Repair → QA ─────────────────────────

    public void StartRepair(RepairStatus inRepairStatus, Guid changedByUserId, string? note = null)
    {
        if (Status.Code is not (RepairStatusCodes.WaitingParts or RepairStatusCodes.OnHold))
            throw new DomainException($"Không thể bắt đầu sửa chữa từ trạng thái hiện tại '{Status.Code}'.");

        ChangeStatus(inRepairStatus, changedByUserId, note);
    }

    public void StartQualityCheck(RepairStatus qaStatus, Guid changedByUserId, string? note = null)
    {
        ChangeStatus(qaStatus, changedByUserId, note);
    }

    public void PassQualityCheck(RepairStatus readyStatus, Guid changedByUserId, string? note = null)
    {
        EnsureHasEverBeenInRepair();

        ChangeStatus(readyStatus, changedByUserId, note);
        CompletedAt = DateTime.UtcNow;
    }

    public void FailQualityCheck(RepairStatus inRepairStatus, Guid changedByUserId, string note)
    {
        if (string.IsNullOrWhiteSpace(note))
            throw new DomainException("Phải ghi chú lý do khi QA không đạt.");

        ChangeStatus(inRepairStatus, changedByUserId, note);
    }

    // ───────────────────────── Delivery & Warranty ─────────────────────────

    public void Deliver(RepairStatus deliveredStatus, Guid changedByUserId, string? note = null)
    {

        ChangeStatus(deliveredStatus, changedByUserId, note);
        DeliveredAt = DateTime.UtcNow;
    }

    public Warranty.Warranty CreateWarranty(DateOnly startDate, DateOnly endDate, string? terms)
    {
        if (Status.Code != RepairStatusCodes.Delivered)
            throw new DomainException("Chỉ tạo bảo hành sau khi thiết bị đã được bàn giao.");
        if (Warranty is not null)
            throw new DomainException("Ticket này đã có thông tin bảo hành.");

        Warranty = new Warranty.Warranty(Id, startDate, endDate, terms);
        return Warranty;
    }

    // ───────────────────────── Inventory ─────────────────────────
    public void UsePart(Part part, Inventory.Inventory inventory, int quantity, Guid changedByUserId)
    {
        if (Status.Code != RepairStatusCodes.InRepair)
            throw new DomainException("Chỉ ghi nhận linh kiện sử dụng khi ticket đang ở trạng thái IN_REPAIR.");

        if (inventory.PartId != part.Id)
            throw new DomainException("Inventory truyền vào không khớp với Part.");

        // BR-20 enforce tại đây — nếu không đủ tồn, KHÔNG tạo TicketPart, ném lỗi rõ ràng cho Application xử lý 409
        if (!inventory.Deduct(quantity))
            throw new InsufficientStockException(part.Name, quantity, inventory.QuantityOnHand);

        var ticketPart = new TicketPart(Id, part.Id, quantity, part.UnitPrice);
        _ticketParts.Add(ticketPart);
        MarkUpdated();
    }

    // ───────────────────────── Helper nội bộ ─────────────────────────

    private void ChangeStatus(RepairStatus newStatus, Guid changedByUserId, string? note)
    {
        RepairTicketStateMachine.EnsureCanTransition(Status.Code, newStatus.Code);

        StatusId = newStatus.Id;
        Status = newStatus;
        _statusHistories.Add(new RepairTicketStatusHistory(Id, newStatus, changedByUserId, note));
        MarkUpdated();
    }

    private void EnsureTechnicianAssigned()
    {
        if (TechnicianId is null)
            throw new DomainException("Ticket chưa được gán kỹ thuật viên.");
    }

    private void EnsureHasEverBeenInRepair()
    {
        if (!_statusHistories.Any(h => h.Status.Code == RepairStatusCodes.InRepair))
            throw new DomainException("Ticket phải từng ở trạng thái IN_REPAIR trước khi được đánh dấu QA đạt.");
    }
}