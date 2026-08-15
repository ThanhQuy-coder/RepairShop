using RepairShop.Domain.Common;

namespace RepairShop.Domain.Modules.Tickets;

public class RepairTicketStatusHistory : BaseEntity
{
    public Guid RepairTicketId { get; private set; }
    public TicketStatus Status { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public string? Note { get; private set; }
    public DateTime ChangedAt { get; private set; }

    private RepairTicketStatusHistory() { } // EF Core

    internal RepairTicketStatusHistory(Guid repairTicketId, TicketStatus status, Guid changedByUserId, string? note)
    {
        Id = Guid.NewGuid();
        RepairTicketId = repairTicketId;
        Status = status;
        ChangedByUserId = changedByUserId;
        Note = note;
        ChangedAt = DateTime.UtcNow;
    }
}