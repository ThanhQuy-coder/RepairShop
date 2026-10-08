using MediatR;
using RepairShop.Application.Modules.Appointments.DTOs;

namespace RepairShop.Application.Modules.Appointments.Commands;

public record CancelAppointmentCommand(Guid Id) : IRequest<AppointmentResponse>;
