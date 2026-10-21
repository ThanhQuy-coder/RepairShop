using MediatR;
using RepairShop.Application.Modules.Appointments.DTOs;

namespace RepairShop.Application.Modules.Appointments.Commands;

public record CancelAppointmentCommand(Guid Id,
    string? LookupToken = null,
    string? Phone = null) : IRequest<AppointmentResponse>;
