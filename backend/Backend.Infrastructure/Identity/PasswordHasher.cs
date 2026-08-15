using RepairShop.Application.Common.Interfaces;

namespace RepairShop.Infrastructure.Identity;

public class PasswordHasher : IPasswordHasher
{
    // Thực hiện băm và xác thực BCrypt
    public string Hash(string plainPassword) =>
        BCrypt.Net.BCrypt.HashPassword(plainPassword);

    public bool Verify(string plainPassword, string hashedPassword) =>
        BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
}