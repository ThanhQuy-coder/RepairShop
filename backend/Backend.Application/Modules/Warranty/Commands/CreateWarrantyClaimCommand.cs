using MediatR;
using RepairShop.Application.Modules.Tickets.DTOs;
using RepairShop.Application.Modules.Appointments.DTOs;

namespace RepairShop.Application.Modules.Warranty.Commands;

public record CreateWarrantyClaimCommand(
    Guid ParentTicketId, string IssueReported, DateOnly AppointmentDate, Guid TimeSlotId)
    : IRequest<AppointmentResponse>;