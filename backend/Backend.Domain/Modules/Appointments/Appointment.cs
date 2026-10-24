using RepairShop.Domain.Common.Exceptions;
using RepairShop.Domain.Modules.Appointments.Enums;
using System.Security.Cryptography;

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
    public string? SerialNumber { get; private set; }
    public string? IssueDescription { get; private set; }
    public DateOnly AppointmentDate { get; private set; }
    public Guid TimeSlotId { get; private set; }
    public AppointmentStatus Status { get; private set; } = AppointmentStatus.Pending;
    public string? RejectionReason { get; private set; } = string.Empty;
    public Guid? ConfirmedByUserId { get; private set; }
    public Guid? HandledByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public string LookupTokenHash { get; private set; } = default!;

    private Appointment() { } // for EF Core

    public Appointment(string fullName,
        string phone,
        DateOnly appointmentDate,
        Guid timeSlotId,
        Guid? customerId = null,
        string? deviceType = null,
        string? brand = null,
        string? model = null,
        string? serialNumber = null,
        string? issueDescription = null,
        string? lookupTokenHash = null)
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
        SerialNumber = serialNumber;
        IssueDescription = issueDescription;
        LookupTokenHash = lookupTokenHash ?? Convert.ToHexString(SHA256.HashData(RandomNumberGenerator.GetBytes(32)));
    }

    public void Confirm(Guid confirmedByUserId)
    {
        if (Status != AppointmentStatus.Pending)
            throw new DomainException($"Chỉ xác nhận được lịch hẹn đang PENDING (hiện tại: {Status}).");

        Status = AppointmentStatus.Confirmed;
        ConfirmedByUserId = confirmedByUserId;
    }

    public void Reject(Guid confirmedByUserId, string rejectionReason)
    {
        if (Status != AppointmentStatus.Pending)
            throw new DomainException($"Chỉ từ chối được lịch hẹn đang PENDING (hiện tại: {Status}).");

        Status = AppointmentStatus.Rejected;
        RejectionReason = rejectionReason;
        ConfirmedByUserId = confirmedByUserId;
    }

    public void Cancel()
    {
        if (Status is not (AppointmentStatus.Pending or AppointmentStatus.Confirmed))
            throw new DomainException($"Không thể hủy lịch hẹn ở trạng thái {Status}.");

        Status = AppointmentStatus.Cancelled;
    }

    public void MarkArrive(Guid userId)
    {
        if (Status != AppointmentStatus.Confirmed)
            throw new DomainException("Chỉ chuyển đổi được lịch hẹn đã CONFIRMED.");

        if (HandledByUserId is null)
            HandledByUserId = userId;

        if (HandledByUserId != userId)
            throw new DomainException("Chỉ nhân viên đã xử lý lịch hẹn mới có thể chuyển đổi sang Ticket.");

        Status = AppointmentStatus.Arrived;
    }

    public void MarkComplete()
    {
        if (Status != AppointmentStatus.Arrived)
            throw new DomainException("Chỉ đánh dấu COMPLETE cho lịch hẹn đã ARRIVED.");

        Status = AppointmentStatus.Complete;
    }

    public void MarkNoShow()
    {
        if (Status != AppointmentStatus.Confirmed)
            throw new DomainException("Chỉ đánh dấu No-show cho lịch hẹn đã CONFIRMED.");

        Status = AppointmentStatus.NoShow;
    }
}