using MediatR;
using RepairShop.Application.Modules.Appointments.DTOs;

namespace RepairShop.Application.Modules.Appointments.Queries;

public sealed record LookupAppointmentQuery(string LookupToken, string Phone) : IRequest<AppointmentResponse>;
