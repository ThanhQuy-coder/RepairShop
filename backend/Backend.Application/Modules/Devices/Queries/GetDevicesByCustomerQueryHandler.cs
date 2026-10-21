using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Devices.DTOs;
using MediatR;
using RepairShop.Domain.Modules.Tickets;

namespace RepairShop.Application.Modules.Devices.Queries;

public class GetDevicesByCustomerQueryHandler : IRequestHandler<GetDevicesByCustomerQuery, List<DeviceResponse>>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IRepairTicketRepository _ticketRepository;

    public GetDevicesByCustomerQueryHandler(IDeviceRepository deviceRepository,
        IRepairTicketRepository ticketRepository)
    {
        _deviceRepository = deviceRepository;
        _ticketRepository = ticketRepository;
    }

    public async Task<List<DeviceResponse>> Handle(GetDevicesByCustomerQuery request, CancellationToken cancellationToken)
    {
        var devices = await _deviceRepository.GetByCustomerIdAsync(request.CustomerId);

        var lockedIds = await _ticketRepository
            .GetDeviceIdsWithActiveTicketAsync(devices.Select(d => d.Id), cancellationToken);

        return devices.Select(d => new DeviceResponse(d.Id, d.CustomerId, d.DeviceType.ToString(),
            d.Brand, d.Model, d.SerialNumber, d.CreatedAt, 
            IsConvertTicket: lockedIds.Contains(d.Id))).ToList();
    }
}