using MediatR;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Appointments.Commands;
using RepairShop.Application.Modules.Appointments.DTOs;
using RepairShop.Application.Modules.Appointments.Mappings;
using RepairShop.Domain.Modules.Appointments;

namespace RepairShop.Application.Modules.Appointments.Queries;

public class GetMyAppointmentsQueryHandler : IRequestHandler<GetMyAppointmentsQuery,
    List<AppointmentResponse>>
{
    private readonly IAppointmentRepository _appointments;
    private readonly ICustomerRepository _customers;
    private readonly ICurrentUserService _currentUser;

    public GetMyAppointmentsQueryHandler(IAppointmentRepository appointments, ICustomerRepository customers,
        ICurrentUserService currentUser)
    {
        _appointments = appointments;
        _customers = customers;
        _currentUser = currentUser;
    }

    public async Task<List<AppointmentResponse>> Handle(GetMyAppointmentsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return [];

        var appointments = await _appointments.GetByPhoneAsync(_currentUser.Phone!);

        if (appointments is null || appointments.Count == 0)
            return [];

        return appointments
            .Select(a => a.ToResponse())
            .ToList();
    }
}
