using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Common.Exceptions;
using RepairShop.Domain.Modules.SLA;
using Xunit;

namespace RepairShop.UnitTests.Domain.SLA;

public sealed class SlaLifecycleTests
{
    [Fact]
    public void Policy_RejectsNonPositiveDuration()
    {
        Assert.Throws<DomainException>(() =>
            new SLAPolicy("DIAGNOSING", DeviceType.Phone, 0));
    }

    [Fact]
    public void Policy_NormalizesStatusCodeAndPreservesDeviceSpecificScope()
    {
        var policy = new SLAPolicy(" diagnosing ", DeviceType.Phone, 90);

        Assert.Equal("DIAGNOSING", policy.StatusCode);
        Assert.Equal(DeviceType.Phone, policy.DeviceType);
        Assert.True(policy.IsActive);
        Assert.Equal(90, policy.DurationMinutes);
    }

    [Fact]
    public void Policy_RejectsStatusOutsideRepairWorkflow()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new SLAPolicy("DELIVERED", null, 60));

        Assert.Contains("workflow", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TicketSla_BecomesDueSoonAtTwentyPercentRemaining()
    {
        var startedAt = DateTime.UtcNow;
        var sla = new TicketSLA(Guid.NewGuid(), Guid.NewGuid(), "DIAGNOSING", startedAt, 100);

        sla.RefreshStatus(startedAt.AddMinutes(80));

        Assert.Equal(SLAStatus.DueSoon, sla.Status);
    }

    [Fact]
    public void TicketSla_UsesCustomDueSoonThreshold()
    {
        var startedAt = DateTime.UtcNow;
        var sla = new TicketSLA(Guid.NewGuid(), Guid.NewGuid(), "DIAGNOSING", startedAt, 100);

        sla.RefreshStatus(startedAt.AddMinutes(70), 40);

        Assert.Equal(SLAStatus.DueSoon, sla.Status);
    }

    [Fact]
    public void TicketSla_OverdueNotificationIsIdempotent()
    {
        var sla = new TicketSLA(Guid.NewGuid(), Guid.NewGuid(), "DIAGNOSING", DateTime.UtcNow, 1);
        var now = DateTime.UtcNow.AddMinutes(2);

        Assert.True(sla.MarkOverdue(now));
        Assert.False(sla.MarkOverdue(now.AddMinutes(1)));
        Assert.Equal(SLAStatus.Overdue, sla.Status);
    }

    [Fact]
    public void TicketSla_ReleaseOverdueNotification_AllowsRetry()
    {
        var sla = new TicketSLA(Guid.NewGuid(), Guid.NewGuid(), "DIAGNOSING",
            DateTime.UtcNow.AddMinutes(-2), 1);
        var now = DateTime.UtcNow;

        Assert.True(sla.MarkOverdue(now));
        sla.ReleaseOverdueNotification();

        Assert.True(sla.MarkOverdue(now.AddMinutes(1)));
    }

    [Fact]
    public void TicketSla_CompletionExcludesItFromActiveLifecycle()
    {
        var sla = new TicketSLA(Guid.NewGuid(), Guid.NewGuid(), "DIAGNOSING", DateTime.UtcNow, 60);

        sla.Complete(DateTime.UtcNow.AddMinutes(10));
        sla.RefreshStatus(DateTime.UtcNow.AddMinutes(100));

        Assert.Equal(SLAStatus.Completed, sla.Status);
    }
}
