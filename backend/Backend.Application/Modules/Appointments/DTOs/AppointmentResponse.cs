namespace RepairShop.Application.Modules.Appointments.DTOs;

public record AppointmentResponse(
    Guid Id,
    Guid? CustomerId,
    string FullName,
    string Phone,
    DateOnly AppointmentDate,
    Guid TimeSlotId,
    string Status,
    DateTime CreatedAt,
    string? LookupToken = null);

public sealed record AppointmentDetailResponse(
    Guid Id,
    string FullName,
    string Phone,
    string? DeviceType,
    string? Brand,
    string? Model,
    string? SerialNumber,
    string? IssueDescription,
    DateOnly AppointmentDate,
    Guid TimeSlotId,
    TimeOnly SlotStart,
    TimeOnly SlotEnd,
    string Status,
    string? RejectionReason,
    DateTime CreatedAt);