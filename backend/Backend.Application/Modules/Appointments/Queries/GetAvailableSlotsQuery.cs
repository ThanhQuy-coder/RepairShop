using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Appointments.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Appointments.Queries;

public record GetAvailableSlotsQuery(DateOnly Date) : IRequest<List<AvailableSlotResponse>>;

public class GetAvailableSlotsQueryHandler : IRequestHandler<GetAvailableSlotsQuery, List<AvailableSlotResponse>>
{
    private readonly ITimeSlotConfigRepository _slotRepo;
    private readonly IAppointmentRepository _appointmentRepo;

    public GetAvailableSlotsQueryHandler(ITimeSlotConfigRepository slotRepo, IAppointmentRepository appointmentRepo)
    {
        _slotRepo = slotRepo;
        _appointmentRepo = appointmentRepo;
    }

    public async Task<List<AvailableSlotResponse>> Handle(GetAvailableSlotsQuery request, CancellationToken cancellationToken)
    {
        var allSlots = await _slotRepo.ListAsync();
        var applicableSlots = allSlots.Where(s => s.AppliesTo(request.Date)).OrderBy(s => s.SlotStart).ToList();

        var result = new List<AvailableSlotResponse>();
        foreach (var slot in applicableSlots)
        {
            var booked = await _appointmentRepo.CountBookedAsync(request.Date, slot.Id);
            var available = Math.Max(0, slot.MaxCapacity - booked);
            result.Add(new AvailableSlotResponse(slot.Id, slot.SlotStart, slot.SlotEnd, slot.MaxCapacity, booked, available, available == 0));
        }
        return result;
    }
}