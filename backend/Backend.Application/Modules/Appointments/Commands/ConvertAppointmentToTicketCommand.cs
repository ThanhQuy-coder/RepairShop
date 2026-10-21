using MediatR;
using RepairShop.Application.Modules.Tickets.DTOs;

namespace RepairShop.Application.Modules.Appointments.Commands;

public record ConvertAppointmentToTicketCommand(Guid AppointmentId, Guid CustomerId, Guid DeviceId) 
    : IRequest<TicketResponse>;
