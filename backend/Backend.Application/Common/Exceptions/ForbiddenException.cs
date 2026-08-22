namespace RepairShop.Application.Common.Exceptions;

/// <summary>403 — khác 401 (chưa xác thực) và khác lỗi Role Policy (đã bị chặn từ tầng [Authorize] trước khi vào Handler).
/// Đây là 403 phát sinh Ở TẦNG NGHIỆP VỤ, sau khi đã qua Role check.</summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}