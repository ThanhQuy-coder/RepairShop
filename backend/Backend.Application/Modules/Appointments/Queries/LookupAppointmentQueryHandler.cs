using System.Security.Cryptography;
using System.Text;
using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Appointments.DTOs;

namespace RepairShop.Application.Modules.Appointments.Queries;

public sealed class LookupAppointmentQueryHandler(IAppointmentRepository repository)
    : IRequestHandler<LookupAppointmentQuery, AppointmentResponse>
{
    public async Task<AppointmentResponse> Handle(LookupAppointmentQuery request, CancellationToken cancellationToken)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.LookupToken)));
        var appointment = await repository.GetByLookupAsync(hash, request.Phone.Trim());
        if (appointment is null)
            throw new NotFoundException("Lịch hẹn", Guid.Empty);

        return new AppointmentResponse(appointment.Id, Guid.Empty, appointment.FullName, appointment.Phone,
            appointment.AppointmentDate, appointment.TimeSlotId, appointment.Status.ToString(), appointment.CreatedAt);
    }
}
