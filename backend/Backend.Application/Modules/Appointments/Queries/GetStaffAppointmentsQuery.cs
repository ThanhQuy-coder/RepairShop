using MediatR;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Appointments.DTOs;

namespace RepairShop.Application.Modules.Appointments.Queries;

public sealed record GetStaffAppointmentsQuery : IRequest<IReadOnlyList<AppointmentResponse>>;

public sealed class GetStaffAppointmentsQueryHandler(IAppointmentRepository repository,
    ICustomerRepository customerRepository)
    : IRequestHandler<GetStaffAppointmentsQuery, IReadOnlyList<AppointmentResponse>>
{
    public async Task<IReadOnlyList<AppointmentResponse>> Handle(
        GetStaffAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var appointments = await repository.GetForStaffAsync();

        var distinctPhones = appointments
            .Select(a => a.Phone)
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct()
            .ToList();

        var phoneToUserIdMap = await customerRepository
            .GetIdsByPhonesAsync(distinctPhones, cancellationToken);

        return appointments
            .Select(a => new AppointmentResponse(
                a.Id,
                phoneToUserIdMap.TryGetValue(a.Phone, out var id) ? id : null,
                a.FullName,
                a.Phone,
                a.AppointmentDate,
                a.TimeSlotId,
                a.Status.ToString(),
                a.CreatedAt))
            .ToList();
    }
}
