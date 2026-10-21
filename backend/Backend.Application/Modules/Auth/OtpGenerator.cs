using System.Security.Cryptography;
using RepairShop.Application.Common.Interfaces;

namespace RepairShop.Application.Modules.Auth;

public sealed class OtpGenerator : IOtpGenerator
{
    public string GenerateNumericOtp()
    {
        int number = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return number.ToString("D6");
    }
}