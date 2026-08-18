using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Exceptions;
using RepairShop.Domain.Modules.Customers;
using RepairShop.Domain.Modules.Devices;
using RepairShop.Domain.Modules.Identity;
using RepairShop.Domain.Modules.Quotes;
using RepairShop.Domain.Modules.Tickets.Enums;
using RepairShop.Domain.Modules.Warranty;

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

    private RepairTicket() { } // for EF Core

    /// <summary>Khởi tạo ticket ở bước tiếp nhận (Task 1 Tuần 1, bước 2-3). Trạng thái ban đầu luôn là CHECKED_IN.</summary>
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

    /// <summary>FR-018: chỉ gán được kỹ thuật viên khi ticket vừa Check-in, chưa từng gán ai.</summary>
    public void AssignTechnician(Guid technicianId, RepairStatus assignedStatus, Guid changedByUserId, string? note = null)
    {
        EnsureCurrentStatusIs(RepairStatusCodes.CheckedIn, "gán kỹ thuật viên");
        EnsureTargetStatusCode(assignedStatus, RepairStatusCodes.Assigned);

        TechnicianId = technicianId;
        ChangeStatus(assignedStatus, changedByUserId, note);
    }

    public void StartDiagnosis(RepairStatus diagnosingStatus, Guid changedByUserId, string? note = null)
    {
        EnsureCurrentStatusIs(RepairStatusCodes.Assigned, "bắt đầu chẩn đoán");
        EnsureTargetStatusCode(diagnosingStatus, RepairStatusCodes.Diagnosing);
        EnsureTechnicianAssigned();

        ChangeStatus(diagnosingStatus, changedByUserId, note);
    }

    /// <summary>FR-024: chỉ ghi được kết quả chẩn đoán khi đang ở bước Diagnosing.</summary>
    public void SubmitDiagnosis(string diagnosisResult)
    {
        EnsureCurrentStatusIs(RepairStatusCodes.Diagnosing, "ghi nhận chẩn đoán");
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

    /// <summary>Chỉ chuyển sang chờ khách duyệt khi đã có kết quả chẩn đoán (đúng thứ tự nghiệp vụ Task 1 Tuần 1).</summary>
    public void SubmitQuote(RepairStatus waitingApprovalStatus, Guid changedByUserId, string? note = null)
    {
        EnsureCurrentStatusIs(RepairStatusCodes.Diagnosing, "gửi báo giá");
        if (string.IsNullOrWhiteSpace(DiagnosisResult))
            throw new DomainException("Phải có kết quả chẩn đoán trước khi gửi báo giá cho khách.");
        EnsureTargetStatusCode(waitingApprovalStatus, RepairStatusCodes.WaitingApproval);

        ChangeStatus(waitingApprovalStatus, changedByUserId, note);
    }

    /// <summary>Đăng ký Quote vào ticket — không tự chuyển trạng thái, gọi kèm SubmitQuote() ở Application layer.</summary>
    public void AttachQuote(Quote quote)
    {
        if (quote.RepairTicketId != Id)
            throw new DomainException("Quote không thuộc về ticket này.");
        _quotes.Add(quote);
    }

    /// <summary>FR-034: khách đồng ý → chuyển sang sửa (có thể là InRepair hoặc WaitingParts, do Application quyết định dựa trên tồn kho).</summary>
    public void ApproveQuote(RepairStatus nextStatus, Guid changedByUserId, string? note = null)
    {
        EnsureCurrentStatusIs(RepairStatusCodes.WaitingApproval, "duyệt báo giá");
        if (nextStatus.Code is not (RepairStatusCodes.InRepair or RepairStatusCodes.WaitingParts))
            throw new DomainException("Sau khi duyệt báo giá chỉ có thể chuyển sang IN_REPAIR hoặc WAITING_PARTS.");

        ChangeStatus(nextStatus, changedByUserId, note);
    }

    /// <summary>FR-035: khách từ chối → đóng ticket.</summary>
    public void RejectQuote(RepairStatus closedStatus, Guid changedByUserId, string reason)
    {
        EnsureCurrentStatusIs(RepairStatusCodes.WaitingApproval, "từ chối báo giá");
        EnsureTargetStatusCode(closedStatus, RepairStatusCodes.ClosedRejected);
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Phải nêu lý do khi từ chối báo giá.");

        ChangeStatus(closedStatus, changedByUserId, reason);
    }

    // ───────────────────────── Workflow: Repair → QA ─────────────────────────

    /// <summary>Dùng khi đủ linh kiện (từ WaitingParts) hoặc bỏ On-hold để tiếp tục sửa.</summary>
    public void StartRepair(RepairStatus inRepairStatus, Guid changedByUserId, string? note = null)
    {
        if (Status.Code is not (RepairStatusCodes.WaitingParts or RepairStatusCodes.OnHold))
            throw new DomainException($"Không thể bắt đầu sửa chữa từ trạng thái hiện tại '{Status.Code}'.");
        EnsureTargetStatusCode(inRepairStatus, RepairStatusCodes.InRepair);

        ChangeStatus(inRepairStatus, changedByUserId, note);
    }

    public void StartQualityCheck(RepairStatus qaStatus, Guid changedByUserId, string? note = null)
    {
        EnsureCurrentStatusIs(RepairStatusCodes.InRepair, "chuyển sang kiểm thử QA");
        EnsureTargetStatusCode(qaStatus, RepairStatusCodes.QaTesting);

        ChangeStatus(qaStatus, changedByUserId, note);
    }

    /// <summary>BR-19: chỉ pass QA khi đã từng thực sự trải qua IN_REPAIR — kiểm tra lại tường minh, không chỉ dựa vào status hiện tại.</summary>
    public void PassQualityCheck(RepairStatus readyStatus, Guid changedByUserId, string? note = null)
    {
        EnsureCurrentStatusIs(RepairStatusCodes.QaTesting, "hoàn tất kiểm thử (đạt)");
        EnsureHasEverBeenInRepair();
        EnsureTargetStatusCode(readyStatus, RepairStatusCodes.ReadyForPickup);

        ChangeStatus(readyStatus, changedByUserId, note);
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>QA không đạt → quay lại sửa (Task 1 Tuần 1, bước 10: "Nếu QA fail → quay lại bước 7").</summary>
    public void FailQualityCheck(RepairStatus inRepairStatus, Guid changedByUserId, string note)
    {
        EnsureCurrentStatusIs(RepairStatusCodes.QaTesting, "ghi nhận QA không đạt");
        EnsureTargetStatusCode(inRepairStatus, RepairStatusCodes.InRepair);
        if (string.IsNullOrWhiteSpace(note))
            throw new DomainException("Phải ghi chú lý do khi QA không đạt.");

        ChangeStatus(inRepairStatus, changedByUserId, note);
    }

    // ───────────────────────── Delivery & Warranty ─────────────────────────

    /// <summary>Bàn giao — chỉ thực hiện được khi đã "Completed" (ReadyForPickup).</summary>
    public void Deliver(RepairStatus deliveredStatus, Guid changedByUserId, string? note = null)
    {
        EnsureCurrentStatusIs(RepairStatusCodes.ReadyForPickup, "bàn giao thiết bị");
        EnsureTargetStatusCode(deliveredStatus, RepairStatusCodes.Delivered);

        ChangeStatus(deliveredStatus, changedByUserId, note);
        DeliveredAt = DateTime.UtcNow;
    }

    /// <summary>BR-10: tối đa 1 Warranty, chỉ tạo sau khi đã Delivered.</summary>
    public Warranty.Warranty CreateWarranty(DateOnly startDate, DateOnly endDate, string? terms)
    {
        if (Status.Code != RepairStatusCodes.Delivered)
            throw new DomainException("Chỉ tạo bảo hành sau khi thiết bị đã được bàn giao.");
        if (Warranty is not null)
            throw new DomainException("Ticket này đã có thông tin bảo hành.");

        Warranty = new Warranty.Warranty(Id, startDate, endDate, terms);
        return Warranty;
    }

    // ───────────────────────── Helper nội bộ ─────────────────────────

    /// <summary>
    /// "Cổng" duy nhất thay đổi StatusId — MỌI method public ở trên đều đi qua đây.
    /// Tự động sinh RepairTicketStatusHistory (BR-05), không có đường nào set StatusId trực tiếp từ bên ngoài.
    /// </summary>
    private void ChangeStatus(RepairStatus newStatus, Guid changedByUserId, string? note)
    {
        StatusId = newStatus.Id;
        Status = newStatus;
        _statusHistories.Add(new RepairTicketStatusHistory(Id, newStatus, changedByUserId, note));
        MarkUpdated();
    }

    private void EnsureCurrentStatusIs(string requiredCode, string actionDescription)
    {
        if (Status.Code != requiredCode)
            throw new DomainException(
                $"Không thể {actionDescription} khi ticket đang ở trạng thái '{Status.Code}' (yêu cầu '{requiredCode}').");
    }

    private void EnsureTargetStatusCode(RepairStatus status, string expectedCode)
    {
        if (status.Code != expectedCode)
            throw new DomainException($"Trạng thái đích phải là '{expectedCode}', nhận được '{status.Code}'.");
    }

    private void EnsureTechnicianAssigned()
    {
        if (TechnicianId is null)
            throw new DomainException("Ticket chưa được gán kỹ thuật viên.");
    }

    /// <summary>BR-19 — enforce tường minh bằng lịch sử thật, không chỉ tin vào Status hiện tại.</summary>
    private void EnsureHasEverBeenInRepair()
    {
        if (!_statusHistories.Any(h => h.Status.Code == RepairStatusCodes.InRepair))
            throw new DomainException("Ticket phải từng ở trạng thái IN_REPAIR trước khi được đánh dấu QA đạt.");
    }
}