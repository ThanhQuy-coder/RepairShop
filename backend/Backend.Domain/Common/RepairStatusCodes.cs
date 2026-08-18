namespace RepairShop.Domain.Common;

public static class RepairStatusCodes
{
    public const string CheckedIn = "CHECKED_IN";
    public const string Assigned = "ASSIGNED";
    public const string Diagnosing = "DIAGNOSING";
    public const string WaitingApproval = "WAITING_APPROVAL";
    public const string OnHold = "ON_HOLD";
    public const string WaitingParts = "WAITING_PARTS";
    public const string InRepair = "IN_REPAIR";
    public const string QaTesting = "QA_TESTING";
    public const string ReadyForPickup = "READY_FOR_PICKUP";
    public const string Delivered = "DELIVERED";
    public const string ClosedRejected = "CLOSED_REJECTED";
}