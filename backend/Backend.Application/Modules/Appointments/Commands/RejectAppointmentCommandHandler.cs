using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Appointments.DTOs;
using RepairShop.Domain.Common;

public class RejectAppointmentCommandHandler : IRequestHandler<RejectAppointmentCommand, AppointmentResponse>
{
    private readonly IAppointmentRepository _repo;
    private readonly ICustomerRepository _customerRepo;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUser;

    public RejectAppointmentCommandHandler(IAppointmentRepository repo, ICustomerRepository customerRepo,
        INotificationService notificationService, ICurrentUserService currentUser)
    {
        _repo = repo;
        _customerRepo = customerRepo;
        _notificationService = notificationService;
        _currentUser = currentUser;
    }

    public async Task<AppointmentResponse> Handle(RejectAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _repo.GetByIdAsync(request.Id) ?? throw new NotFoundException("Lịch hẹn", request.Id);

        appointment.Reject(_currentUser.UserId!.Value);
        _repo.Update(appointment);

        if (appointment.CustomerId is not null)
        {
            var customer = await _customerRepo.GetByIdAsync(appointment.CustomerId.Value);
            if (customer?.UserId is not null)
                await _notificationService.CreateAsync(customer.UserId.Value, NotificationTypes.AppointmentRejected,
                    "Lịch hẹn bị từ chối",
                    $"Lịch hẹn ngày {appointment.AppointmentDate:dd/MM/yyyy} của bạn đã bị từ chối. Vui lòng liên hệ cửa hàng.",
                    "Appointment", appointment.Id);
        }

        await _repo.SaveChangesAsync();
        return new AppointmentResponse(appointment.Id, appointment.FullName, appointment.Phone,
            appointment.AppointmentDate, appointment.TimeSlotId, appointment.Status.ToString(), appointment.CreatedAt);
    }
}