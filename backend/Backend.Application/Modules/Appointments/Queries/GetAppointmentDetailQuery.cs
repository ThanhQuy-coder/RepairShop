using MediatR;
using RepairShop.Application.Modules.Appointments.DTOs;

namespace RepairShop.Application.Modules.Appointments.Queries;

public sealed record GetAppointmentDetailQuery(Guid Id) : IRequest<AppointmentDetailResponse>;
