using RepairShop.Domain.Common;
using RepairShop.Domain.Modules.Tickets;
using Xunit;

namespace RepairShop.UnitTests.Domain.Tickets;

public class RepairTicketStateMachineRegressionTests
{
    // ───────────────────────── Valid transitions (happy path đầy đủ) ─────────────────────────

    [Theory]
    [InlineData(RepairStatusCodes.CheckedIn, RepairStatusCodes.Assigned)]
    [InlineData(RepairStatusCodes.Assigned, RepairStatusCodes.Diagnosing)]
    [InlineData(RepairStatusCodes.Diagnosing, RepairStatusCodes.WaitingApproval)]
    [InlineData(RepairStatusCodes.WaitingApproval, RepairStatusCodes.InRepair)]
    [InlineData(RepairStatusCodes.WaitingApproval, RepairStatusCodes.WaitingParts)]
    [InlineData(RepairStatusCodes.WaitingApproval, RepairStatusCodes.ClosedRejected)]
    [InlineData(RepairStatusCodes.WaitingApproval, RepairStatusCodes.OnHold)]
    [InlineData(RepairStatusCodes.OnHold, RepairStatusCodes.WaitingApproval)]
    [InlineData(RepairStatusCodes.WaitingParts, RepairStatusCodes.InRepair)]
    [InlineData(RepairStatusCodes.InRepair, RepairStatusCodes.QaTesting)]
    [InlineData(RepairStatusCodes.QaTesting, RepairStatusCodes.ReadyForPickup)]
    [InlineData(RepairStatusCodes.QaTesting, RepairStatusCodes.InRepair)] // QA fail
    [InlineData(RepairStatusCodes.ReadyForPickup, RepairStatusCodes.Delivered)]
    public void ValidTransitions_AreAllowed(string from, string to)
    {
        Assert.True(RepairTicketStateMachine.CanTransition(from, to));
    }

    // ───────────────────────── Invalid transitions (nhảy cóc) ─────────────────────────

    [Theory]
    [InlineData(RepairStatusCodes.CheckedIn, RepairStatusCodes.Diagnosing)]
    [InlineData(RepairStatusCodes.CheckedIn, RepairStatusCodes.WaitingApproval)]
    [InlineData(RepairStatusCodes.CheckedIn, RepairStatusCodes.InRepair)]
    [InlineData(RepairStatusCodes.CheckedIn, RepairStatusCodes.QaTesting)]
    [InlineData(RepairStatusCodes.CheckedIn, RepairStatusCodes.ReadyForPickup)]
    [InlineData(RepairStatusCodes.CheckedIn, RepairStatusCodes.Delivered)]
    [InlineData(RepairStatusCodes.Assigned, RepairStatusCodes.WaitingApproval)]
    [InlineData(RepairStatusCodes.Assigned, RepairStatusCodes.InRepair)]
    [InlineData(RepairStatusCodes.Diagnosing, RepairStatusCodes.ReadyForPickup)] // ví dụ mentor nhấn mạnh Task 4.7
    [InlineData(RepairStatusCodes.InRepair, RepairStatusCodes.CheckedIn)] // đi lùi
    [InlineData(RepairStatusCodes.InRepair, RepairStatusCodes.ReadyForPickup)] // bỏ qua QA
    [InlineData(RepairStatusCodes.Delivered, RepairStatusCodes.InRepair)] // terminal không đi tiếp
    [InlineData(RepairStatusCodes.ClosedRejected, RepairStatusCodes.InRepair)] // terminal không đi tiếp
    [InlineData(RepairStatusCodes.Delivered, RepairStatusCodes.CheckedIn)]
    public void InvalidTransitions_AreRejected(string from, string to)
    {
        Assert.False(RepairTicketStateMachine.CanTransition(from, to));
    }

    [Fact]
    public void EnsureCanTransition_ThrowsDomainException_ForInvalidTransition()
    {
        var ex = Assert.Throws<RepairShop.Domain.Common.Exceptions.DomainException>(
            () => RepairTicketStateMachine.EnsureCanTransition(RepairStatusCodes.CheckedIn, RepairStatusCodes.ReadyForPickup));

        Assert.Contains("CHECKED_IN", ex.Message);
        Assert.Contains("READY_FOR_PICKUP", ex.Message);
    }

    // ───────────────────────── Terminal states có ĐÚNG 0 transition đi tiếp ─────────────────────────

    [Theory]
    [InlineData(RepairStatusCodes.Delivered)]
    [InlineData(RepairStatusCodes.ClosedRejected)]
    public void TerminalStates_HaveNoOutgoingTransitions(string terminalStatus)
    {
        var allStatuses = new[]
        {
            RepairStatusCodes.CheckedIn, RepairStatusCodes.Assigned, RepairStatusCodes.Diagnosing,
            RepairStatusCodes.WaitingApproval, RepairStatusCodes.OnHold, RepairStatusCodes.WaitingParts,
            RepairStatusCodes.InRepair, RepairStatusCodes.QaTesting, RepairStatusCodes.ReadyForPickup,
            RepairStatusCodes.Delivered, RepairStatusCodes.ClosedRejected,
        };

        foreach (var target in allStatuses)
        {
            Assert.False(RepairTicketStateMachine.CanTransition(terminalStatus, target),
                $"{terminalStatus} không được phép chuyển sang {target} — đây là trạng thái kết thúc.");
        }
    }
}