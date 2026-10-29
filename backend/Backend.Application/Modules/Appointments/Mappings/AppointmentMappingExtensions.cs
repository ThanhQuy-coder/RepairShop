using RepairShop.Application.Modules.Appointments.DTOs;
using RepairShop.Domain.Modules.Appointments;

namespace RepairShop.Application.Modules.Appointments.Mappings;

public static class AppointmentMappingExtensions
{
    /// <summary>
    /// Map Appointment entity -> AppointmentResponse.
    /// LookupToken mặc định KHÔNG trả về (nhạy cảm); chỉ bật khi thực sự cần
    /// (vd. ngay sau khi tạo appointment để FE hiển thị link tra cứu).
    /// </summary>
    public static AppointmentResponse ToResponse(
        this Appointment appointment,
        bool includeLookupToken = false,
        string lookupToken = null!)
        => new(
            appointment.Id,
            appointment.CustomerId,
            appointment.ConfirmedByUserId,
            appointment.HandledByUserId,
            appointment.FullName,
            appointment.Phone,
            appointment.AppointmentDate,
            appointment.TimeSlotId,
            appointment.Status.ToString(),
            appointment.CreatedAt,
            includeLookupToken ? lookupToken : null);

}