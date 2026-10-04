namespace RepairShop.Application.Modules.Appointments.DTOs;

public record AvailableSlotResponse(Guid TimeSlotId, TimeOnly SlotStart, TimeOnly SlotEnd, int MaxCapacity, int Booked, int Available, bool IsFull);