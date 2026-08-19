namespace RepairShop.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entityName, Guid id)
        : base($"Không tìm thấy {entityName} với Id '{id}'.") { }
}