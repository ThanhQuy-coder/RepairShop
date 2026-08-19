namespace RepairShop.Application.Common.Interfaces;

/// <summary>
/// Sinh Ticket Code — tách interface riêng vì thuật toán sinh mã có thể cần biết
/// database (kiểm tra trùng) hoặc chỉ cần random đủ mạnh; để Infrastructure tự quyết định cách làm.
/// </summary>
public interface ITicketCodeGenerator
{
    Task<string> GenerateUniqueCodeAsync();
}