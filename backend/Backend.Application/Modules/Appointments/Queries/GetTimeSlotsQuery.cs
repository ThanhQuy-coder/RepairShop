using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Appointments.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Appointments.Queries;

public record GetTimeSlotsQuery : IRequest<List<TimeSlotResponse>>;

public class GetTimeSlotsQueryHandler : IRequestHandler<GetTimeSlotsQuery, List<TimeSlotResponse>>
{
    private readonly ITimeSlotConfigRepository _repo;
    public GetTimeSlotsQueryHandler(ITimeSlotConfigRepository repo) => _repo = repo;

    public async Task<List<TimeSlotResponse>> Handle(GetTimeSlotsQuery request, CancellationToken cancellationToken)
    {
        var slots = await _repo.ListAsync();
        return slots.Select(s => new TimeSlotResponse(s.Id, s.DayOfWeek, s.SlotStart, s.SlotEnd, s.MaxCapacity, s.IsActive)).ToList();
    }
}