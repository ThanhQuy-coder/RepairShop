using MediatR;
using RepairShop.Application.Modules.Appointments.DTOs;

namespace RepairShop.Application.Modules.Appointments.Commands;

public record ConfirmAppointmentCommand(Guid Id) : IRequest<AppointmentResponse>;