namespace RepairShop.Domain.Common;

/// <summary>
/// Hằng số loại thông báo — dùng string thay vì enum để tránh phải migration đổi kiểu cột
/// mỗi khi thêm loại mới (SLA, Inventory... ở các Tuần sau). Giữ tập trung tại đây để không
/// gõ tay chuỗi rải rác, tương tự cách RepairStatusCodes đã làm cho RepairTicket.
/// </summary>
public static class NotificationTypes
{
    public const string AppointmentCreated = "AppointmentCreated";
    public const string AppointmentConfirmed = "AppointmentConfirmed";
    public const string AppointmentRejected = "AppointmentRejected";
}