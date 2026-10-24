using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Appointments.Commands;
using RepairShop.Application.Modules.Tickets.Commands;
using RepairShop.Application.Modules.Tickets.DTOs;
using RepairShop.Domain.Common.Exceptions;

public class ConvertAppointmentToTicketCommandHandler
    : IRequestHandler<ConvertAppointmentToTicketCommand, TicketResponse>
{
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ConvertAppointmentToTicketCommandHandler(IAppointmentRepository appointmentRepo,
        IMediator mediator, IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _appointmentRepo = appointmentRepo;
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

        TicketResponse? ticket = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            ticket = await _mediator.Send(new CreateTicketCommand(
                request.CustomerId, request.DeviceId,
                appointment.IssueDescription ?? "Theo lịch hẹn đã đặt trước",
                Notes: null, ConditionNotes: null, RiskWarning: null, DiagnosticDeposit: 0), cancellationToken);

            _appointmentRepo.Update(appointment);
            await _appointmentRepo.SaveChangesAsync();
        }, cancellationToken);

        return ticket!;
    }
}