using MediatR;
using Microsoft.Extensions.Logging;
using RepairShop.Application.Common.Authorization;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Appointments.Commands;
using RepairShop.Application.Modules.Appointments.DTOs;
using RepairShop.Domain.Modules.Appointments.Enums;
using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Exceptions;

namespace RepairShop.Application.Modules.Warranty.Commands;

public class CreateWarrantyClaimCommandHandler(
    IRepairTicketRepository ticketRepository,
    ICustomerRepository customerRepository,
    IAppointmentRepository appointmentRepository,
    ICurrentUserService currentUser,
    IMediator mediator,
    ILogger<CreateWarrantyClaimCommandHandler> logger)
    : IRequestHandler<CreateWarrantyClaimCommand, AppointmentResponse>
{
    public async Task<AppointmentResponse> Handle(
        CreateWarrantyClaimCommand request, CancellationToken cancellationToken)
    {
        var parentTicket = await ticketRepository.GetByIdAsync(request.ParentTicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa gốc", request.ParentTicketId);

        if (currentUser.Role == Roles.Customer)
        {
            var customer = await customerRepository.GetByUserIdAsync(currentUser.UserId!.Value);
            TicketAccessGuard.EnsureCustomerOwnsTicket(parentTicket, customer);
        }

        parentTicket.EnsureCanCreateWarrantyClaim();
        if (await appointmentRepository.HasOpenWarrantyAppointmentAsync(parentTicket.Id))
            throw new DomainException("Phiếu này đã có một lịch hẹn bảo hành đang xử lý.");
        var customerRecord = await customerRepository.GetByIdAsync(parentTicket.CustomerId)
            ?? throw new NotFoundException("Khách hàng", parentTicket.CustomerId);
        var appointment = await mediator.Send(new CreateAppointmentCommand(
            customerRecord.FullName,
            customerRecord.Phone,
            request.AppointmentDate,
            request.TimeSlotId,
            parentTicket.CustomerId,
            parentTicket.Device.DeviceType.ToString(),
            parentTicket.Device.Brand,
            parentTicket.Device.Model,
            request.IssueReported.Trim(),
            parentTicket.Device.SerialNumber,
            AppointmentType.WarrantyRevisit,
            parentTicket.Id), cancellationToken);

        logger.LogInformation("Tạo lịch hẹn bảo hành {AppointmentId} cho ticket gốc {ParentTicketId}.",
            appointment.Id, parentTicket.Id);
        return appointment;
    }
}
