namespace Backend.Domain.Common;

/// <summary>
/// Hằng số tên Role — PHẢI khớp chính xác dữ liệu seed ở RoleConfiguration (Task 5/6).
/// Dùng thay cho việc gõ tay "Admin", "Receptionist" rải rác khắp Controller (dễ gõ sai chính tả, khó refactor).
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Receptionist = "Receptionist";
    public const string Technician = "Technician";
    public const string Customer = "Customer";
}