namespace RepairShop.Application.Modules.Appointments.DTOs;

public record TimeSlotResponse(Guid Id, int? DayOfWeek, TimeOnly SlotStart, TimeOnly SlotEnd, int MaxCapacity, bool IsActive);