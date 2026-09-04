using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Tickets.DTOs;
using RepairShop.Domain.Common;

namespace RepairShop.Application.Modules.Devices.Queries;

public record GetDeviceRepairHistoryQuery(Guid DeviceId) : IRequest<List<TicketListItemResponse>>;

public class GetDeviceRepairHistoryQueryHandler : IRequestHandler<GetDeviceRepairHistoryQuery, List<TicketListItemResponse>>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICurrentUserService _currentUser;

    public GetDeviceRepairHistoryQueryHandler(
        IRepairTicketRepository ticketRepository,
        IDeviceRepository deviceRepository,
        ICustomerRepository customerRepository,
        ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _deviceRepository = deviceRepository;
        _customerRepository = customerRepository;
        _currentUser = currentUser;
    }

    public async Task<List<TicketListItemResponse>> Handle(
        GetDeviceRepairHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetByIdAsync(request.DeviceId)
            ?? throw new NotFoundException("Thiết bị", request.DeviceId);

        if (_currentUser.Role == Roles.Customer)
        {
            var customer = _currentUser.UserId is null
                ? null
                : await _customerRepository.GetByUserIdAsync(_currentUser.UserId.Value);
            if (customer is null || customer.Id != device.CustomerId)
                throw new ForbiddenException("Bạn không có quyền xem lịch sử thiết bị này.");
        }

        var tickets = await _ticketRepository.GetByDeviceForHistoryAsync(request.DeviceId);
        return tickets.Select(ticket => new TicketListItemResponse(
            ticket.Id,
            ticket.TicketCode,
            ticket.Customer.FullName,
            $"{ticket.Device.Brand} {ticket.Device.Model}",
            ticket.Technician?.FullName,
            ticket.Status.Code,
            ticket.ReceivedAt,
            ticket.IssueReported)).ToList();
    }
}
