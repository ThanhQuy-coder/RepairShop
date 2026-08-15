namespace RepairShop.Domain.Modules.Tickets;

public enum TicketStatus
{
    CheckedIn = 1,
    Assigned = 2,
    Diagnosing = 3,
    WaitingApproval = 4,
    OnHold = 5,
    WaitingParts = 6,
    InRepair = 7,
    QaTesting = 8,
    ReadyForPickup = 9,
    Delivered = 10,
    ClosedRejected = 11
}

public static class TicketStatusExtensions
{
    private static readonly Dictionary<TicketStatus, TicketStatus[]> AllowedTransitions = new()
    {
        [TicketStatus.CheckedIn] = new[] { TicketStatus.Assigned, TicketStatus.OnHold },
        [TicketStatus.Assigned] = new[] { TicketStatus.Diagnosing, TicketStatus.OnHold },
        [TicketStatus.Diagnosing] = new[] { TicketStatus.WaitingApproval, TicketStatus.OnHold },
        [TicketStatus.WaitingApproval] = new[] { TicketStatus.WaitingParts, TicketStatus.ClosedRejected, TicketStatus.OnHold },
        [TicketStatus.WaitingParts] = new[] { TicketStatus.InRepair, TicketStatus.OnHold },
        [TicketStatus.OnHold] = new[]
        {
            TicketStatus.CheckedIn, TicketStatus.Assigned, TicketStatus.Diagnosing,
            TicketStatus.WaitingApproval, TicketStatus.WaitingParts, TicketStatus.InRepair
        },
        [TicketStatus.InRepair] = new[] { TicketStatus.QaTesting, TicketStatus.OnHold },
        [TicketStatus.QaTesting] = new[] { TicketStatus.ReadyForPickup, TicketStatus.InRepair },
        [TicketStatus.ReadyForPickup] = new[] { TicketStatus.Delivered },
        [TicketStatus.Delivered] = Array.Empty<TicketStatus>(),
        [TicketStatus.ClosedRejected] = Array.Empty<TicketStatus>()
    };

    // Kiểm tra luồng chuyển trạng thái trước khi lưu vào DB
    public static bool CanTransitionTo(this TicketStatus current, TicketStatus target)
        => AllowedTransitions.TryGetValue(current, out var allowed) && allowed.Contains(target);

    // Kiểm tra trạng thái kết thúc
    public static bool IsTerminal(this TicketStatus status)
        => status is TicketStatus.Delivered or TicketStatus.ClosedRejected;
}