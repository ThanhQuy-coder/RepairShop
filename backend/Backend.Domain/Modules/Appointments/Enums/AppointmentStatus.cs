namespace RepairShop.Domain.Modules.Appointments.Enums;

public enum AppointmentStatus
{
    Pending,
    Confirmed,
    Rejected, // Nhân viên từ chối
    Arrived,
    Complete,
    Cancelled, // Khách hàng hủy lịch hẹn
    NoShow // Nhân viên đánh dấu KH không đến
}