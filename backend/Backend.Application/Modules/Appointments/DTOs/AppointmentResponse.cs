namespace RepairShop.Application.Modules.Appointments.DTOs;

public record AppointmentResponse(Guid Id, string FullName, string Phone, DateOnly AppointmentDate, Guid TimeSlotId, string Status, DateTime CreatedAt);