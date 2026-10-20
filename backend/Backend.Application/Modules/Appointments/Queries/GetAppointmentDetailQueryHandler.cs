using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Appointments.DTOs;

namespace RepairShop.Application.Modules.Appointments.Queries;

public sealed class GetAppointmentDetailQueryHandler : IRequestHandler<GetAppointmentDetailQuery, AppointmentDetailResponse>
{
    private readonly IAppointmentRepository _repository;

    public GetAppointmentDetailQueryHandler(IAppointmentRepository repository)
        => _repository = repository;

    public async Task<AppointmentDetailResponse> Handle(GetAppointmentDetailQuery request, CancellationToken cancellationToken)
    {
        var appointment = await _repository.GetDetailByIdAsync(request.Id)
            ?? throw new NotFoundException("Lịch hẹn", request.Id);

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
