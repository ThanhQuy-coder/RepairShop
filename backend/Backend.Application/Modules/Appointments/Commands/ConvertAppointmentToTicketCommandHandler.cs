using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Tickets.Commands;
using RepairShop.Application.Modules.Tickets.DTOs;

public class ConvertAppointmentToTicketCommandHandler : IRequestHandler<ConvertAppointmentToTicketCommand, TicketResponse>
{
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public ConvertAppointmentToTicketCommandHandler(IAppointmentRepository appointmentRepo, IMediator mediator, IUnitOfWork unitOfWork)
    {
        _appointmentRepo = appointmentRepo;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public async Task<TicketResponse> Handle(ConvertAppointmentToTicketCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepo.GetByIdAsync(request.AppointmentId)
            ?? throw new NotFoundException("Lịch hẹn", request.AppointmentId);

        // Domain tự chặn: không CONFIRMED thì không convert (checklist "chỉ convert CONFIRMED"),
        // và đã LinkedTicketId rồi thì không convert lần 2 (checklist "không convert lần 2") — cả 2
        // điều kiện enforce bên trong ConvertToTicket(), không lặp lại logic ở Application.
        TicketResponse? ticket = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Tái dùng NGUYÊN VẸN CreateTicketCommandHandler đã có (Task 4.3) — không viết lại logic
            // sinh TicketCode, validate Customer/Device, khởi tạo RepairTicket ở trạng thái CHECKED_IN.
            ticket = await _mediator.Send(new CreateTicketCommand(
                request.CustomerId, request.DeviceId,
                appointment.IssueDescription ?? "Theo lịch hẹn đã đặt trước",
                Notes: null, ConditionNotes: null, RiskWarning: null, DiagnosticDeposit: 0), cancellationToken);

            appointment.ConvertToTicket(ticket.Id); // gán LinkedTicketId + đổi status CONVERTED trong Domain
            _appointmentRepo.Update(appointment);
            await _appointmentRepo.SaveChangesAsync();
        }, cancellationToken);

        return ticket!;
    }
}