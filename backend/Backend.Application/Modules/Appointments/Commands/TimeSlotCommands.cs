using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Appointments.DTOs;
using RepairShop.Domain.Modules.Appointments;
using MediatR;

namespace RepairShop.Application.Modules.Appointments.Commands;

public record CreateTimeSlotCommand(int? DayOfWeek, TimeOnly SlotStart, TimeOnly SlotEnd, int MaxCapacity) : IRequest<TimeSlotResponse>;

public class CreateTimeSlotCommandHandler : IRequestHandler<CreateTimeSlotCommand, TimeSlotResponse>
{
    private readonly ITimeSlotConfigRepository _repo;
    public CreateTimeSlotCommandHandler(ITimeSlotConfigRepository repo) => _repo = repo;

    public async Task<TimeSlotResponse> Handle(CreateTimeSlotCommand request, CancellationToken cancellationToken)
    {
        var slot = new TimeSlotConfig(request.SlotStart, request.SlotEnd, request.MaxCapacity, request.DayOfWeek);
        await _repo.AddAsync(slot);
        await _repo.SaveChangesAsync();
        return new TimeSlotResponse(slot.Id, slot.DayOfWeek, slot.SlotStart, slot.SlotEnd, slot.MaxCapacity, slot.IsActive);
    }
}

public record UpdateTimeSlotCommand(Guid Id, int? DayOfWeek, TimeOnly SlotStart, TimeOnly SlotEnd, int MaxCapacity) : IRequest<TimeSlotResponse>;

public class UpdateTimeSlotCommandHandler : IRequestHandler<UpdateTimeSlotCommand, TimeSlotResponse>
{
    private readonly ITimeSlotConfigRepository _repo;
    public UpdateTimeSlotCommandHandler(ITimeSlotConfigRepository repo) => _repo = repo;

    public async Task<TimeSlotResponse> Handle(UpdateTimeSlotCommand request, CancellationToken cancellationToken)
    {
        var slot = await _repo.GetByIdAsync(request.Id) ?? throw new NotFoundException("Khung giờ", request.Id);
        slot.UpdateInfo(request.SlotStart, request.SlotEnd, request.MaxCapacity, request.DayOfWeek);
        _repo.Update(slot);
        await _repo.SaveChangesAsync();
        return new TimeSlotResponse(slot.Id, slot.DayOfWeek, slot.SlotStart, slot.SlotEnd, slot.MaxCapacity, slot.IsActive);
    }
}

public record DeleteTimeSlotCommand(Guid Id) : IRequest<Unit>;

public class DeleteTimeSlotCommandHandler : IRequestHandler<DeleteTimeSlotCommand, Unit>
{
    private readonly ITimeSlotConfigRepository _repo;
    public DeleteTimeSlotCommandHandler(ITimeSlotConfigRepository repo) => _repo = repo;

    public async Task<Unit> Handle(DeleteTimeSlotCommand request, CancellationToken cancellationToken)
    {
        var slot = await _repo.GetByIdAsync(request.Id) ?? throw new NotFoundException("Khung giờ", request.Id);
        // Soft-remove qua Deactivate thay vì xóa cứng — an toàn hơn nếu đã có Appointment tham chiếu (FK Restrict)
        slot.Deactivate();
        _repo.Update(slot);
        await _repo.SaveChangesAsync();
        return Unit.Value;
    }
}