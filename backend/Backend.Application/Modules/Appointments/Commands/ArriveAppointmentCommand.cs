using MediatR;
using RepairShop.Application.Modules.Appointments.DTOs;

namespace RepairShop.Domain.Modules.Appointments;

public record class ArriveAppointmentCommand(Guid AppointmentId) 
    : IRequest<AppointmentResponse>;