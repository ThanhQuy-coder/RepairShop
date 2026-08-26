namespace RepairShop.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entityName, Guid id)
        : base($"Không tìm thấy {entityName} với Id '{id}'.") { }

    public NotFoundException(string entityName, string identifier)
        : base($"Không tìm thấy {entityName} với mã '{identifier}'.") { }
}