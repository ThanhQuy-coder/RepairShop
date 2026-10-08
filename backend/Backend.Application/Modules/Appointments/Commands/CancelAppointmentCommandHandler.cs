using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Appointments.DTOs;

namespace RepairShop.Application.Modules.Appointments.Commands;

public class CancelAppointmentCommandHandler : IRequestHandler<CancelAppointmentCommand, AppointmentResponse>
{
    private readonly IAppointmentRepository _appointments;
    private readonly ICustomerRepository _customers;
    private readonly ICurrentUserService _currentUser;

    public CancelAppointmentCommandHandler(IAppointmentRepository appointments, ICustomerRepository customers,
        ICurrentUserService currentUser)
    {
        _appointments = appointments;
        _customers = customers;
        _currentUser = currentUser;
    }

    public async Task<AppointmentResponse> Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointments.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Lịch hẹn", request.Id);
        var customer = await _customers.GetByUserIdAsync(_currentUser.UserId!.Value)
            ?? throw new NotFoundException("Khách hàng", _currentUser.UserId.Value);
        if (appointment.CustomerId != customer.Id)
            throw new ForbiddenException("Bạn không có quyền hủy lịch hẹn này.");

        appointment.Cancel();
        _appointments.Update(appointment);
        await _appointments.SaveChangesAsync();
        return ToResponse(appointment);
    }

    internal static AppointmentResponse ToResponse(Domain.Modules.Appointments.Appointment appointment) =>
        new(appointment.Id, appointment.FullName, appointment.Phone, appointment.AppointmentDate,
            appointment.TimeSlotId, appointment.Status.ToString(), appointment.CreatedAt);
}
