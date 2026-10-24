using System.Security.Cryptography;
using System.Text;
using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Appointments.DTOs;
using RepairShop.Application.Modules.Appointments.Mappings;
using RepairShop.Domain.Modules.Appointments;
using RepairShop.Domain.Modules.Appointments.Enums;

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

        if (appointment.Status is AppointmentStatus.Cancelled)
            throw new Exception("Không thể huỷ lịch hẹn ở trạng thái này.");

        await EnsureCanCancel(appointment, request);
        appointment.Cancel();
        _appointments.Update(appointment);
        await _appointments.SaveChangesAsync();
        return appointment.ToResponse();
    }

    private async Task EnsureCanCancel(Appointment appointment, CancelAppointmentCommand request)
    {
        // 1) Khách đã đăng nhập và appointment thuộc về họ → OK
        if (_currentUser.UserId is Guid userId)
        {
            var customer = await _customers.GetByUserIdAsync(userId);
            if (customer is not null && appointment.CustomerId == customer.Id)
                return;
        }

        // 2) Fallback: verify bằng token + phone (khách vãng lai, hoặc khách đã đăng nhập
        //    nhưng appointment tạo từ lúc chưa đăng nhập)
        if (string.IsNullOrWhiteSpace(request.LookupToken)
            || string.IsNullOrWhiteSpace(request.Phone))
            throw new ForbiddenException("Vui lòng đăng nhập hoặc cung cấp mã tra cứu và số điện thoại.");

        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(request.LookupToken)));

        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(appointment.LookupTokenHash),
                Convert.FromHexString(hash))
            || appointment.Phone != request.Phone)
            throw new ForbiddenException("Thông tin xác thực không đúng.");
    }
}
