using MediatR;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Appointments.Commands;
using RepairShop.Application.Modules.Appointments.DTOs;

namespace RepairShop.Application.Modules.Appointments.Queries;

public class GetMyAppointmentsQueryHandler : IRequestHandler<GetMyAppointmentsQuery, List<AppointmentResponse>>
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
        var customer = await _customers.GetByUserIdAsync(_currentUser.UserId!.Value);
        if (customer is null) return [];
        var appointments = await _appointments.GetByCustomerIdAsync(customer.Id);
        return appointments.Select(CancelAppointmentCommandHandler.ToResponse).ToList();
    }
}
