using Backend.Domain.Common;
using Backend.Domain.Modules.Customers;
using Backend.Domain.Modules.Devices;
using Backend.Domain.Modules.Identity;
using Backend.Domain.Modules.Quotes;
using Backend.Domain.Modules.Warranty;

namespace Backend.Domain.Modules.Tickets;

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
    public decimal DiagnosticDeposit { get; private set; } = 0;
    public Guid? ParentTicketId { get; private set; } // liên kết ticket bảo hành (BR-12)

    public Customer Customer { get; private set; } = default!;
    public Device Device { get; private set; } = default!;
    public User Receptionist { get; private set; } = default!;
    public User? Technician { get; private set; }
    public RepairStatus Status { get; private set; } = default!;
    public RepairTicket? ParentTicket { get; private set; }
    public ICollection<RepairTicket> WarrantyTickets { get; private set; } = new List<RepairTicket>();
    public ICollection<Quote> Quotes { get; private set; } = new List<Quote>();
    public Warranty.Warranty? Warranty { get; private set; }

    private RepairTicket() { } // for EF Core

    public RepairTicket(string ticketCode, Guid customerId, Guid deviceId, Guid receptionistId,
        string issueReported, int initialStatusId, decimal diagnosticDeposit = 0, Guid? parentTicketId = null)
    {
        if (string.IsNullOrWhiteSpace(issueReported))
            throw new DomainException("Mô tả lỗi (IssueReported) không được để trống.");

        if (diagnosticDeposit < 0)
            throw new DomainException("Tiền cọc chẩn đoán không thể âm.");

        // TODO: Ticket không thể tự liên kết chính nó (Chưa kiểm tra)

        TicketCode = ticketCode;
        CustomerId = customerId;
        DeviceId = deviceId;
        ReceptionistId = receptionistId;
        IssueReported = issueReported;
        StatusId = initialStatusId;
        DiagnosticDeposit = diagnosticDeposit;
        ParentTicketId = parentTicketId;
    }

    public void AssignTechnician(Guid technicianId)
    {
        TechnicianId = technicianId;
        MarkUpdated();
    }

    public void SubmitDiagnosis(string diagnosisResult)
    {
        DiagnosisResult = diagnosisResult;
        MarkUpdated();
    }

    // Logic nghiệp vụ đầy đủ (ChangeStatus ghi StatusHistory, SubmitQAResult enforce BR-19...)
    // sẽ hoàn thiện ở Task "Xây dựng nghiệp vụ Workflow" (Tuần 4) — hiện chỉ dựng entity + property.
}