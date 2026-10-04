using RepairShop.Application.Modules.Appointments.Commands;
using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Appointments.DTOs;
using RepairShop.Domain.Common;

public class ConfirmAppointmentCommandHandler : IRequestHandler<ConfirmAppointmentCommand, AppointmentResponse>
{
    private readonly IAppointmentRepository _repo;
    private readonly ICustomerRepository _customerRepo;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUser;

    public ConfirmAppointmentCommandHandler(IAppointmentRepository repo, ICustomerRepository customerRepo,
        INotificationService notificationService, ICurrentUserService currentUser)
    {
        _repo = repo;
        _customerRepo = customerRepo;
        _notificationService = notificationService;
        _currentUser = currentUser;
    }

    public async Task<AppointmentResponse> Handle(ConfirmAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _repo.GetByIdAsync(request.Id) ?? throw new NotFoundException("Lịch hẹn", request.Id);

        // Domain tự chặn nếu KHÔNG phải PENDING (bao gồm cả Cancelled/Rejected/Converted/NoShow)
        appointment.Confirm(_currentUser.UserId!.Value);
        _repo.Update(appointment);

        await NotifyCustomerIfLinked(appointment.CustomerId,
            NotificationTypes.AppointmentConfirmed, "Lịch hẹn đã được xác nhận",
            $"Lịch hẹn ngày {appointment.AppointmentDate:dd/MM/yyyy} của bạn đã được cửa hàng xác nhận.", appointment.Id);
        await _repo.SaveChangesAsync();
        return Map(appointment);
    }

    private async Task NotifyCustomerIfLinked(Guid? customerId, string type, string title, string message, Guid relatedId)
    {
        if (customerId is null) return; // khách vãng lai, không có tài khoản để thông báo trong hệ thống

        var customer = await _customerRepo.GetByIdAsync(customerId.Value);
        if (customer?.UserId is null) return; // Customer tồn tại nhưng chưa từng liên kết User

        await _notificationService.CreateAsync(customer.UserId.Value, type, title, message, "Appointment", relatedId);
    }

    private static AppointmentResponse Map(RepairShop.Domain.Modules.Appointments.Appointment a) =>
        new(a.Id, a.FullName, a.Phone, a.AppointmentDate, a.TimeSlotId, a.Status.ToString(), a.CreatedAt);
}