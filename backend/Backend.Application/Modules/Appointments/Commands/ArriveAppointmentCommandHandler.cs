using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Appointments.DTOs;
using RepairShop.Application.Modules.Appointments.Mappings;
using RepairShop.Domain.Modules.Appointments;

namespace RepairShop.Application.Modules.Appointments.Commands;

public class ArriveAppointmentCommandHandler : IRequestHandler<ArriveAppointmentCommand, AppointmentResponse>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ArriveAppointmentCommandHandler(IAppointmentRepository appointmentRepository,
        IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _appointmentRepository = appointmentRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<AppointmentResponse> Handle(ArriveAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId)
            ?? throw new NotFoundException("Lịch hẹn", request.AppointmentId);

        appointment.MarkArrive(_currentUserService.UserId!.Value);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            _appointmentRepository.Update(appointment);
            await _appointmentRepository.SaveChangesAsync();
        }, cancellationToken);

        return appointment.ToResponse();

    }
}