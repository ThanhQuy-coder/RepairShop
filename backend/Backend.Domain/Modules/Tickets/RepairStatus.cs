namespace Backend.Domain.Modules.Tickets;

/// <summary>
/// Dữ liệu mẫu: CHECKED_IN, ASSIGNED, DIAGNOSING, WAITING_APPROVAL, ON_HOLD,
/// WAITING_PARTS, IN_REPAIR, QA_TESTING, READY_FOR_PICKUP, DELIVERED, CLOSED_REJECTED.
/// </summary>
public class RepairStatus
{
    public int Id { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public int SortOrder { get; private set; }

    private RepairStatus() { } // for EF Core

    public RepairStatus(int id, string code, string name, int sortOrder)
    {
        Id = id;
        Code = code;
        Name = name;
        SortOrder = sortOrder;
    }
}