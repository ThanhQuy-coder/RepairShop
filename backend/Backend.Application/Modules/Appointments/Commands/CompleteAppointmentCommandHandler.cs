using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Appointments.DTOs;
using RepairShop.Application.Modules.Appointments.Mappings;
using RepairShop.Domain.Modules.Appointments;

namespace RepairShop.Application.Modules.Appointments.Commands;

public class CompleteAppointmentCommandHandler
    : IRequestHandler<CompleteAppointmentCommand, AppointmentResponse>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly INotificationService _notificationService;

    public CompleteAppointmentCommandHandler(IAppointmentRepository appointmentRepository,
        INotificationService notificationService)
    {
        _appointmentRepository = appointmentRepository;
        _notificationService = notificationService;
    }


    public async Task<AppointmentResponse> Handle(CompleteAppointmentCommand request,
         CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId)
            ?? throw new NotFoundException("Lịch hẹn", request.AppointmentId);

        if (appointment == null)
        {
            throw new NotFoundException($"Appointment with ID {request.AppointmentId} not found.",
                request.AppointmentId);
        }

        appointment.MarkComplete();
        _appointmentRepository.Update(appointment);

        await _appointmentRepository.SaveChangesAsync();

        await _notificationService.CreateAsync((Guid) appointment.CustomerId!, "AppointmentCompleted",
            "Lịch hẹn đã hoàn tất",
            $"Lịch hẹn của bạn vào ngày {appointment.AppointmentDate:dd/MM/yyyy} đã được hoàn tất.",
            "Appointment", appointment.Id);

        return appointment.ToResponse();
    }
}