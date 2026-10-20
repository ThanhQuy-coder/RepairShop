using System.Security.Cryptography;
using System.Text;
using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Appointments.DTOs;

namespace RepairShop.Application.Modules.Appointments.Queries;

public sealed class LookupAppointmentQueryHandler(IAppointmentRepository repository)
    : IRequestHandler<LookupAppointmentQuery, AppointmentDetailResponse>
{
    public async Task<AppointmentDetailResponse> Handle(LookupAppointmentQuery request, CancellationToken cancellationToken)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.LookupToken)));
        var appointment = await repository.GetByLookupAsync(hash, request.Phone.Trim());
        if (appointment is null)
            throw new NotFoundException("Lịch hẹn", Guid.Empty);

        return new AppointmentDetailResponse(
            appointment.Id,
            appointment.FullName,
            appointment.Phone,
            appointment.DeviceType,
            appointment.Brand,
            appointment.Model,
            appointment.SerialNumber,
            appointment.IssueDescription,
            appointment.AppointmentDate,
            appointment.TimeSlotId,
            appointment.SlotStart,
            appointment.SlotEnd,
            appointment.Status.ToString(),
            appointment.RejectionReason,
            appointment.CreatedAt);
    }
}
