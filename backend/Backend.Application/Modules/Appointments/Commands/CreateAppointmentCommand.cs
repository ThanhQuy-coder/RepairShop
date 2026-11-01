using MediatR;
using RepairShop.Application.Modules.Appointments.DTOs;
using RepairShop.Domain.Modules.Appointments.Enums;

namespace RepairShop.Application.Modules.Appointments.Commands;

public record CreateAppointmentCommand(
    string FullName, string Phone, DateOnly AppointmentDate, Guid TimeSlotId,
    Guid? CustomerId, string? DeviceType, string? Brand, string? Model, string? IssueDescription, string? Serial,
    AppointmentType Type = AppointmentType.Repair, Guid? ParentTicketId = null)
    : IRequest<AppointmentResponse>;