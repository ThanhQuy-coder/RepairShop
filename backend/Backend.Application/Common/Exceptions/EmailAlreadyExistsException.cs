namespace RepairShop.Application.Common.Exceptions;

public class EmailAlreadyExistsException : Exception
{
    public EmailAlreadyExistsException(string email)
        : base($"Email '{email}' đã được đăng ký.") { }
}