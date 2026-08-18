namespace RepairShop.Domain.Common.Exceptions;

public class InsufficientStockException : DomainException
{
    public InsufficientStockException(string partName, int requested, int available)
        : base($"Không đủ tồn kho linh kiện '{partName}'. Yêu cầu {requested}, hiện còn {available}.") { }
}