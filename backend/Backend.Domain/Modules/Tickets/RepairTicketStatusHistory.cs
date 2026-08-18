using RepairShop.Domain.Modules.Identity;

namespace RepairShop.Domain.Modules.Tickets;

public class RepairTicketStatusHistory
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid RepairTicketId { get; private set; }
    public int StatusId { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public string? Note { get; private set; }
    public DateTime ChangedAt { get; private set; } = DateTime.UtcNow;

    public RepairStatus Status { get; private set; } = default!;
    public User ChangedByUser { get; private set; } = default!;

    private RepairTicketStatusHistory() { }

    internal RepairTicketStatusHistory(Guid repairTicketId, RepairStatus status, Guid changedByUserId, string? note)
    {
        RepairTicketId = repairTicketId;
        StatusId = status.Id;
        Status = status;
        ChangedByUserId = changedByUserId;
        Note = note;
    }
}