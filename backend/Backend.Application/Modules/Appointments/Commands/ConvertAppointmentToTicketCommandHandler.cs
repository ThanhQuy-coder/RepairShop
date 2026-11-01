using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Appointments.Commands;
using RepairShop.Application.Modules.Tickets.Commands;
using RepairShop.Application.Modules.Tickets.DTOs;
using RepairShop.Domain.Common.Exceptions;
using RepairShop.Domain.Modules.Appointments.Enums;
using RepairShop.Domain.Modules.Appointments;

public class ConvertAppointmentToTicketCommandHandler
    : IRequestHandler<ConvertAppointmentToTicketCommand, TicketResponse>
{
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IRepairTicketRepository _ticketRepo;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ConvertAppointmentToTicketCommandHandler(IAppointmentRepository appointmentRepo,
    IRepairTicketRepository ticketRepo,
        IMediator mediator, IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _appointmentRepo = appointmentRepo;
        _ticketRepo = ticketRepo;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<TicketResponse> Handle(ConvertAppointmentToTicketCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepo.GetByIdAsync(request.AppointmentId)
            ?? throw new NotFoundException("Lịch hẹn", request.AppointmentId);

        if (appointment.HandledByUserId != _currentUser.UserId)
            throw new DomainException("Chỉ nhân viên đã xử lý lịch hẹn mới có thể chuyển sang phiếu sửa chữa.");

        if (appointment.Status != AppointmentStatus.Arrived)
            throw new DomainException("Lịch hẹn này đã được tiếp nhận hoặc chưa đến bước tiếp nhận.");

        if (appointment.Type == AppointmentType.WarrantyRevisit && appointment.ParentTicketId is null)
            throw new DomainException("Lịch hẹn bảo hành chưa liên kết phiếu gốc.");

        var customerId = request.CustomerId;
        var deviceId = request.DeviceId;
        Guid? parentTicketId = appointment.ParentTicketId;

        if (appointment.Type == AppointmentType.WarrantyRevisit)
        {
            var parent = await _appointmentRepo.GetParentTicketAsync(appointment.ParentTicketId!.Value)
                ?? throw new NotFoundException("Phiếu sửa chữa gốc", appointment.ParentTicketId.Value);
            customerId = parent.CustomerId;
            deviceId = parent.DeviceId;
        }

        if (customerId is null || deviceId is null)
            throw new DomainException("Cần xác định khách hàng và thiết bị trước khi tạo phiếu.");

        TicketResponse? ticket = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _appointmentRepo.AcquireConversionLockAsync(appointment.Id, cancellationToken);

            if (appointment.Type == AppointmentType.WarrantyRevisit &&
                await _ticketRepo.GetWarrantyChildByParentTicketIdAsync(appointment.ParentTicketId!.Value) is not null)
                throw new DomainException("Lịch hẹn bảo hành này đã được tiếp nhận, không thể tạo thêm phiếu.");

            ticket = await _mediator.Send(new CreateTicketCommand(
                customerId.Value, deviceId.Value,
                appointment.IssueDescription ?? "Theo lịch hẹn đã đặt trước",
                Notes: null, ConditionNotes: null, RiskWarning: null, DiagnosticDeposit: 0,
                ParentTicketId: parentTicketId), cancellationToken);

            appointment.MarkComplete();
            _appointmentRepo.Update(appointment);
            await _appointmentRepo.SaveChangesAsync();
        }, cancellationToken);

        return ticket!;
    }
}