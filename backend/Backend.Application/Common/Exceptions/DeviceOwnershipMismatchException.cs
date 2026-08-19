namespace RepairShop.Application.Common.Exceptions;

/// <summary>Enforce Acceptance Criteria: "Không cho Customer A sử dụng Device của Customer B".</summary>
public class DeviceOwnershipMismatchException : Exception
{
    public DeviceOwnershipMismatchException(Guid deviceId, Guid customerId)
        : base($"Thiết bị '{deviceId}' không thuộc về khách hàng '{customerId}'.") { }
}