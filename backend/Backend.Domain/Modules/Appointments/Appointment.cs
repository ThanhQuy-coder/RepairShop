using RepairShop.Domain.Common.Exceptions;
using RepairShop.Domain.Modules.Appointments.Enums;

namespace RepairShop.Domain.Modules.Appointments;

public class Appointment
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid? CustomerId { get; private set; }
    public string FullName { get; private set; } = default!;
    public string Phone { get; private set; } = default!;
    public string? DeviceType { get; private set; }
    public string? Brand { get; private set; }
    public string? Model { get; private set; }
    public string? IssueDescription { get; private set; }
    public DateOnly AppointmentDate { get; private set; }
    public Guid TimeSlotId { get; private set; }
    public AppointmentStatus Status { get; private set; } = AppointmentStatus.Pending;
    public Guid? ConfirmedByUserId { get; private set; }
    public Guid? LinkedTicketId { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private Appointment() { } // for EF Core

    public Appointment(string fullName, string phone, DateOnly appointmentDate, Guid timeSlotId,
        Guid? customerId = null, string? deviceType = null, string? brand = null, string? model = null,
        string? issueDescription = null)
    {
        if (string.IsNullOrWhiteSpace(fullName)) throw new DomainException("Họ tên không được để trống.");
        if (string.IsNullOrWhiteSpace(phone)) throw new DomainException("Số điện thoại không được để trống.");
        if (appointmentDate < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new DomainException("Ngày hẹn không thể ở quá khứ.");

        FullName = fullName;
        Phone = phone;
        AppointmentDate = appointmentDate;
        TimeSlotId = timeSlotId;
        CustomerId = customerId;
        DeviceType = deviceType;
        Brand = brand;
        Model = model;
        IssueDescription = issueDescription;
    }

    /// <summary>BR-21 tinh thần: Receptionist/Admin xác nhận lịch hẹn.</summary>
    public void Confirm(Guid confirmedByUserId)
    {
        if (Status != AppointmentStatus.Pending)
            throw new DomainException($"Chỉ xác nhận được lịch hẹn đang PENDING (hiện tại: {Status}).");

        Status = AppointmentStatus.Confirmed;
        ConfirmedByUserId = confirmedByUserId;
    }

    public void Reject(Guid confirmedByUserId)
    {
        if (Status != AppointmentStatus.Pending)
            throw new DomainException($"Chỉ từ chối được lịch hẹn đang PENDING (hiện tại: {Status}).");

        Status = AppointmentStatus.Rejected;
        ConfirmedByUserId = confirmedByUserId;
    }

    /// <summary>Customer hủy lịch hẹn chưa xác nhận hoặc còn trong hạn hủy.</summary>
    public void Cancel()
    {
        if (Status is not (AppointmentStatus.Pending or AppointmentStatus.Confirmed))
            throw new DomainException($"Không thể hủy lịch hẹn ở trạng thái {Status}.");

        Status = AppointmentStatus.Cancelled;
    }

    /// <summary>BR-22: chỉ Appointment đã CONFIRMED mới convert được, và chỉ convert đúng 1 lần.</summary>
    public void ConvertToTicket(Guid ticketId)
    {
        if (Status != AppointmentStatus.Confirmed)
            throw new DomainException("Chỉ chuyển đổi được lịch hẹn đã CONFIRMED.");
        if (LinkedTicketId is not null)
            throw new DomainException("Lịch hẹn này đã được chuyển đổi thành phiếu sửa chữa trước đó.");

        LinkedTicketId = ticketId;
        Status = AppointmentStatus.Converted;
    }

    public void MarkNoShow()
    {
        if (Status != AppointmentStatus.Confirmed)
            throw new DomainException("Chỉ đánh dấu No-show cho lịch hẹn đã CONFIRMED.");

        Status = AppointmentStatus.NoShow;
    }
}