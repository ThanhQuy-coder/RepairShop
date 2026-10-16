using MediatR;
using RepairShop.Application.Modules.Appointments.DTOs;

public record RejectAppointmentCommand(Guid Id, string RejectionReason) : IRequest<AppointmentResponse>;