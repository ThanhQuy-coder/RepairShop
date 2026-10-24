using MediatR;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Appointments.DTOs;
using RepairShop.Application.Modules.Appointments.Mappings;

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
            .Select(a =>
            {
                var customerId = phoneToUserIdMap.TryGetValue(a.Phone, out var id) ? (Guid?)id : null;
                return a.ToResponse() with { CustomerId = customerId };
            })
            .ToList();
    }
}
