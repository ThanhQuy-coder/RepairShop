using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Appointments.DTOs;

namespace RepairShop.Application.Modules.Appointments.Commands;

public class MarkNoShowAppointmentCommandHandler : IRequestHandler<MarkNoShowAppointmentCommand, AppointmentResponse>
{
    private readonly IAppointmentRepository _repo;
    private readonly ICustomerRepository _customerRepo;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUser;

    public MarkNoShowAppointmentCommandHandler(
        IAppointmentRepository repo,
        ICustomerRepository customerRepo,
        INotificationService notificationService,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _customerRepo = customerRepo;
        _notificationService = notificationService;
        _currentUser = currentUser;
    }

    public async Task<AppointmentResponse> Handle(MarkNoShowAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _repo.GetByIdAsync(request.AppointmentId)
            ?? throw new NotFoundException("Lịch hẹn", request.AppointmentId);

        appointment.MarkNoShow();
        _repo.Update(appointment);

        if (appointment.CustomerId is not null)
        {
            var customer = await _customerRepo.GetByIdAsync(appointment.CustomerId.Value);
            if (customer?.UserId is not null)
                await _notificationService.CreateAsync(customer.UserId.Value, "AppointmentNoShow",
                    "Bạn đã không đến",
                    $"Lịch hẹn ngày {appointment.AppointmentDate:dd/MM/yyyy} của bạn đã bị đánh dấu là không đến. Vui lòng liên hệ cửa hàng.",
                    "Appointment", appointment.Id);
        }

        await _repo.SaveChangesAsync();
        return new AppointmentResponse(appointment.Id, Guid.Empty,
            appointment.FullName,
            appointment.Phone,
            appointment.AppointmentDate,
            appointment.TimeSlotId,
            appointment.Status.ToString(),
            appointment.CreatedAt);
    }
}