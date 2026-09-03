using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Exceptions;
using RepairShop.Domain.Modules.Billing;
using RepairShop.Domain.Modules.Billing.Enums;
using RepairShop.Domain.Modules.Customers;
using RepairShop.Domain.Modules.Devices;
using RepairShop.Domain.Modules.Identity;
using RepairShop.Domain.Modules.Inventory;
using RepairShop.Domain.Modules.Quotes;
using RepairShop.Domain.Modules.Tickets.Enums;

namespace RepairShop.Domain.Modules.Tickets;

public class RepairTicket : BaseEntity
{
    private const int MaxImagesPerTicket = 5;
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

    public string? ConditionNotes { get; private set; }
    public string? RiskWarning { get; private set; }

    public string? RootCause { get; private set; }
    public string? RecommendedRepair { get; private set; }
    public string? RequiredPartsNote { get; private set; }

    public string? CompletionNotes { get; private set; }

    public Invoice? Invoice { get; private set; }

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
    /// <summary>
    /// FR-017/FR-026. Ràng buộc theo loại ảnh:
    /// - BEFORE_REPAIR: chỉ hợp lý trước khi bắt đầu sửa (từ CHECKED_IN đến trước IN_REPAIR).
    /// - AFTER_REPAIR: chỉ hợp lý sau khi đã/đang sửa (từ IN_REPAIR trở đi).
    /// - OTHER: không ràng buộc trạng thái, nhưng vẫn chặn ở 2 trạng thái kết thúc (đã đóng hồ sơ).
    /// </summary>
    public void AddImage(string imageUrl, ImageType imageType, Guid uploadedByUserId, string? caption = null)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new DomainException("URL ảnh không được để trống.");

        if (_images.Count >= MaxImagesPerTicket)
            throw new DomainException($"Mỗi phiếu sửa chữa chỉ được lưu tối đa {MaxImagesPerTicket} ảnh.");

        var terminalStatuses = new[] { RepairStatusCodes.Delivered, RepairStatusCodes.ClosedRejected };
        if (terminalStatuses.Contains(Status.Code))
            throw new DomainException("Không thể thêm ảnh khi ticket đã kết thúc (đã bàn giao hoặc đã đóng).");

        var beforeRepairAllowedStatuses = new[]
        {
            RepairStatusCodes.CheckedIn, RepairStatusCodes.Assigned, RepairStatusCodes.Diagnosing,
            RepairStatusCodes.WaitingApproval, RepairStatusCodes.OnHold, RepairStatusCodes.WaitingParts
        };

        if (imageType == ImageType.BeforeRepair && !beforeRepairAllowedStatuses.Contains(Status.Code))
            throw new DomainException("Ảnh BEFORE_REPAIR chỉ được thêm trước khi bắt đầu sửa chữa.");

        if (imageType == ImageType.AfterRepair &&
            Status.Code is not (RepairStatusCodes.InRepair or RepairStatusCodes.QaTesting or RepairStatusCodes.ReadyForPickup))
            throw new DomainException("Ảnh AFTER_REPAIR chỉ được thêm khi đang/đã sửa chữa xong.");

        _images.Add(new TicketImage(Id, imageUrl, imageType, uploadedByUserId, caption));
        MarkUpdated();
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

    /// <summary>
    /// Ghi đè lại SubmitDiagnosis() từ Task 4.1 — mở rộng đủ field mentor yêu cầu.
    /// TechnicalNotes tái dùng field Notes/AddNote() đã có sẵn (Task 4.1) — không tạo field trùng lặp,
    /// vì "ghi chú kỹ thuật trong quá trình xử lý" về bản chất là cùng khái niệm với Notes chung của ticket.
    /// </summary>
    public void SubmitDiagnosis(string diagnosisResult, string? rootCause, string? recommendedRepair,
        string? requiredPartsNote)
    {
        if (Status.Code != RepairStatusCodes.Diagnosing)
            throw new DomainException(
                $"Chỉ ghi nhận chẩn đoán khi ticket ở trạng thái DIAGNOSING (hiện tại: '{Status.Code}').");
        if (string.IsNullOrWhiteSpace(diagnosisResult))
            throw new DomainException("Kết quả chẩn đoán không được để trống.");

        DiagnosisResult = diagnosisResult;
        RootCause = rootCause;
        RecommendedRepair = recommendedRepair;
        RequiredPartsNote = requiredPartsNote;
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

    /// <summary>
    /// Deliver() (đã có từ Task 4.1/4.2) — bổ sung gate: KHÔNG cho bàn giao nếu chưa thanh toán.
    /// Đây là business rule hợp lý dù Tuần 4 chỉ mock payment: mock không có nghĩa là bỏ qua ràng buộc
    /// nghiệp vụ "phải thu tiền trước khi trả máy", chỉ là không tích hợp cổng thanh toán thật.
    /// </summary>
    public void Deliver(RepairStatus deliveredStatus, Guid changedByUserId, string? note = null)
    {
        if (Invoice is null)
            throw new DomainException("Phải xuất hóa đơn trước khi bàn giao thiết bị.");
        if (Invoice.PaidAt is null)
            throw new DomainException("Hóa đơn chưa được thanh toán, không thể bàn giao thiết bị.");

        ChangeStatus(deliveredStatus, changedByUserId, note); // enforce transition READY_FOR_PICKUP -> DELIVERED
        DeliveredAt = DateTime.UtcNow;
    }

    /// <summary>
    /// BR-10 (tối đa 1 Warranty) + Business Rule quan trọng của Task 4.14:
    /// "Warranty không nên tồn tại trước khi Ticket hoàn tất/bàn giao" — enforce cứng bằng điều kiện
    /// Status.Code == DELIVERED. Đây KHÔNG phải kiểm tra tùy chọn, mà là điều kiện BẮT BUỘC đầu tiên.
    /// </summary>
    public Warranty.Warranty CreateWarranty(string warrantyCode, DateOnly startDate, DateOnly endDate, string? terms)
    {
        if (Status.Code != RepairStatusCodes.Delivered)
            throw new DomainException("Chỉ tạo thông tin bảo hành sau khi thiết bị đã được bàn giao (DELIVERED).");
        if (Warranty is not null)
            throw new DomainException("Ticket này đã có thông tin bảo hành.");

        Warranty = new Warranty.Warranty(warrantyCode, Id, startDate, endDate, terms);
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

    /// <summary>
    /// Ghi bản ghi StatusHistory ĐẦU TIÊN khi ticket vừa khởi tạo — khác ChangeStatus() vì đây
    /// không phải "chuyển" từ trạng thái nào cả, chỉ là xác nhận trạng thái CHECKED_IN ban đầu.
    /// Gọi đúng 1 lần ngay sau constructor, ở Application layer (không thể gọi từ trong constructor
    /// vì lúc đó Id có thể chưa được EF Core gán xong tuỳ chiến lược sinh khoá).
    /// </summary>
    public void RecordInitialStatusHistory(Guid receptionistId)
    {
        if (_statusHistories.Count > 0)
            throw new DomainException("Ticket đã có lịch sử trạng thái, không thể ghi lại lịch sử khởi tạo.");

        _statusHistories.Add(new RepairTicketStatusHistory(Id, Status, receptionistId, "Tiếp nhận thiết bị"));
    }

    /// <summary>
    /// Ghi nhận tình trạng máy lúc tiếp nhận (Task 1 Tuần 1, bước 2). Chỉ cho phép sửa khi ticket
    /// còn ở CHECKED_IN — sau khi đã Assign kỹ thuật viên, thông tin này KHÔNG được sửa nữa
    /// (đúng lý do gốc: "ghi chú vết trầy/móp có sẵn để tránh tranh chấp sau này" — nếu cho sửa tuỳ tiện
    /// thì mất luôn giá trị làm bằng chứng).
    /// </summary>
    public void RecordIntakeCondition(string? conditionNotes, string? riskWarning)
    {
        if (Status.Code != RepairStatusCodes.CheckedIn)
            throw new DomainException(
                "Chỉ được ghi/sửa tình trạng tiếp nhận khi ticket còn ở trạng thái CHECKED_IN.");

        ConditionNotes = conditionNotes;
        RiskWarning = riskWarning;
        MarkUpdated();
    }

    /// <summary>Technician tổng kết công việc đã làm — bắt buộc phải có trước khi chuyển sang QA (xem StartQualityCheck bên dưới).</summary>
    public void RecordCompletionNotes(string completionNotes)
    {
        if (Status.Code != RepairStatusCodes.InRepair)
            throw new DomainException("Chỉ ghi nhận ghi chú hoàn tất khi ticket đang ở trạng thái IN_REPAIR.");
        if (string.IsNullOrWhiteSpace(completionNotes))
            throw new DomainException("Ghi chú hoàn tất không được để trống.");

        CompletionNotes = completionNotes;
        MarkUpdated();
    }

    /// <summary>Chỉ phát lệnh xuất hóa đơn khi ticket đã READY_FOR_PICKUP (BR-09: tối đa 1 Invoice).</summary>
    public Invoice CreateInvoice(PaymentMethod paymentMethod, decimal totalAmount,
        Guid createdByUserId, Guid? quoteId = null)
    {
        if (Status.Code != RepairStatusCodes.ReadyForPickup)
            throw new DomainException("Chỉ xuất hóa đơn khi thiết bị đã sẵn sàng bàn giao (READY_FOR_PICKUP).");
        if (Invoice is not null)
            throw new DomainException("Ticket này đã có hóa đơn.");

        Invoice = new Invoice(Id, quoteId, totalAmount, paymentMethod, createdByUserId);
        return Invoice;
    }
}