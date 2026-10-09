using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Modules.Tickets;

namespace RepairShop.Domain.Modules.SLA;

public sealed class TicketSLA : BaseEntity
{
    public Guid RepairTicketId { get; private set; }
    public Guid SLAPolicyId { get; private set; }
    public string StatusCode { get; private set; } = default!;
    public DateTime StartedAt { get; private set; }
    public DateTime DueAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? OverdueNotifiedAt { get; private set; }
    public SLAStatus Status { get; private set; }
    public RepairTicket RepairTicket { get; private set; } = default!;
    public SLAPolicy SLAPolicy { get; private set; } = default!;

    private TicketSLA() { }

    public TicketSLA(Guid ticketId, Guid policyId, string statusCode, DateTime startedAt, int durationMinutes)
    {
        RepairTicketId = ticketId;
        SLAPolicyId = policyId;
        StatusCode = statusCode;
        StartedAt = startedAt;
        DueAt = startedAt.AddMinutes(durationMinutes);
        Status = SLAStatus.OnTrack;
    }

    public void Complete(DateTime completedAt)
    {
        if (Status == SLAStatus.Completed) return;
        CompletedAt = completedAt;
        Status = SLAStatus.Completed;
        MarkUpdated();
    }

    public bool MarkOverdue(DateTime now)
    {
        if (Status == SLAStatus.Completed || now < DueAt || OverdueNotifiedAt.HasValue) return false;
        Status = SLAStatus.Overdue;
        OverdueNotifiedAt = now;
        MarkUpdated();
        return true;
    }

    public void ReleaseOverdueNotification()
    {
        if (Status == SLAStatus.Overdue)
            OverdueNotifiedAt = null;
    }

    public void RefreshStatus(DateTime now)
    {
        if (Status == SLAStatus.Completed) return;
        Status = now >= DueAt
            ? SLAStatus.Overdue
            : now >= DueAt.AddMinutes(-(DueAt - StartedAt).TotalMinutes * 0.2)
                ? SLAStatus.DueSoon
                : SLAStatus.OnTrack;
        MarkUpdated();
    }
}
