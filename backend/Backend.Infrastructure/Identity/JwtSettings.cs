namespace RepairShop.Infrastructure.Identity;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = default!;
    public string Issuer { get; set; } = default!; // Nhà phát hành token
    public string Audience { get; set; } = default!; // Đối tượng sử dụng
    public int ExpiryMinutes { get; set; } = 60;
}