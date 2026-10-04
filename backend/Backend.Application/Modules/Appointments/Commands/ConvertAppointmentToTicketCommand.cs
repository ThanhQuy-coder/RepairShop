using MediatR;
using RepairShop.Application.Modules.Tickets.DTOs;

public record ConvertAppointmentToTicketCommand(Guid AppointmentId, Guid CustomerId, Guid DeviceId) : IRequest<TicketResponse>;
