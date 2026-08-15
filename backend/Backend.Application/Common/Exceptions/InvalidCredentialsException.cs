namespace RepairShop.Application.Common.Exceptions;

public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("Email hoặc mật khẩu không chính xác.") { }
}