using MediatR;
using RepairShop.Application.Modules.Appointments.DTOs;

namespace RepairShop.Application.Modules.Appointments.Queries;

public record GetMyAppointmentsQuery : IRequest<List<AppointmentResponse>>;
