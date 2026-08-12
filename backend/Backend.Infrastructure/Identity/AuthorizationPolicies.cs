using Backend.Domain.Common;

namespace Backend.Infrastructure.Identity;

/// <summary>
/// Tên các Policy dùng trong [Authorize(Policy = "...")].
/// Mỗi Policy map tới 1 tổ hợp Role cụ thể trong nghiệp vụ (tham chiếu Task 2/3 Tuần 1 - Actor & FR).
/// </summary>
public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string StaffOnly = "StaffOnly";              // Admin + Receptionist + Technician (mọi nhân viên nội bộ)
    public const string ReceptionistOrAdmin = "ReceptionistOrAdmin";
    public const string TechnicianOrAdmin = "TechnicianOrAdmin";
    public const string InventoryViewers = "InventoryViewers"; // FR-045: Technician + Admin xem tồn kho
}