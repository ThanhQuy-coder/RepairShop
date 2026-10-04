using MediatR;
using RepairShop.Application.Modules.Appointments.DTOs;

namespace RepairShop.Application.Modules.Appointments.Commands;

public record CreateAppointmentCommand(
    string FullName, string Phone, DateOnly AppointmentDate, Guid TimeSlotId,
    Guid? CustomerId, string? DeviceType, string? Brand, string? Model, string? IssueDescription)
    : IRequest<AppointmentResponse>;