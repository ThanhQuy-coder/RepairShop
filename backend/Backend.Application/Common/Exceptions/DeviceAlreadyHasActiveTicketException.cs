namespace RepairShop.Application.Common.Exceptions;

public class DeviceAlreadyHasActiveTicketException : Exception
{
    public DeviceAlreadyHasActiveTicketException(Guid deviceId, string ticketCode)
        : base($"Thiết bị có ID '{deviceId}' đã có một phiếu sửa chữa đang hoạt động với mã '{ticketCode}'.")
    {
    }

    public DeviceAlreadyHasActiveTicketException(Guid deviceId)
        : base($"Thiết bị có ID '{deviceId}' đã có một phiếu sửa chữa đang hoạt động.")
    {
    }
}