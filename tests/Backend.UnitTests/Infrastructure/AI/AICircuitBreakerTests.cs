using RepairShop.Infrastructure.AI;
using Xunit;

namespace RepairShop.UnitTests.Infrastructure.AI;

public class AICircuitBreakerTests
{
    [Fact]
    public void InitialState_IsClosed_AllowsExecution()
    {
        var breaker = new AICircuitBreaker();
        Assert.True(breaker.CanExecute());
        Assert.Equal(CircuitState.Closed, breaker.CurrentState);
    }

    [Fact]
    public void ThreeConsecutiveFailures_OpensCircuit()
    {
        var breaker = new AICircuitBreaker();

        breaker.RecordFailure();
        breaker.RecordFailure();
        Assert.Equal(CircuitState.Closed, breaker.CurrentState); // chưa đủ ngưỡng

        breaker.RecordFailure(); // lỗi thứ 3 -> mở circuit
        Assert.Equal(CircuitState.Open, breaker.CurrentState);
        Assert.False(breaker.CanExecute()); // trong thời gian cooldown -> chặn ngay
    }

    [Fact]
    public void SuccessAfterFailures_ResetsCircuit_WhileStillClosed()
    {
        var breaker = new AICircuitBreaker();
        breaker.RecordFailure();
        breaker.RecordFailure();

        breaker.RecordSuccess(); // thành công trước khi đủ ngưỡng -> reset đếm lỗi

        breaker.RecordFailure();
        breaker.RecordFailure();
        Assert.Equal(CircuitState.Closed, breaker.CurrentState); // vẫn chưa đủ 3 lỗi LIÊN TIẾP mới sau reset
    }

    [Fact]
    public void HalfOpen_SuccessfulProbe_ClosesCircuit()
    {
        var breaker = new AICircuitBreaker();
        breaker.RecordFailure();
        breaker.RecordFailure();
        breaker.RecordFailure(); // -> OPEN

        // Giả lập đã qua cooldown bằng cách set field private qua reflection (chỉ dùng cho test)
        typeof(AICircuitBreaker)
            .GetField("_openedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(breaker, DateTime.UtcNow.AddSeconds(-31));

        Assert.True(breaker.CanExecute()); // chuyển sang HALF_OPEN, cho thử
        Assert.Equal(CircuitState.HalfOpen, breaker.CurrentState);

        breaker.RecordSuccess(); // thử thành công
        Assert.Equal(CircuitState.Closed, breaker.CurrentState);
    }

    [Fact]
    public void HalfOpen_FailedProbe_ReopensCircuit()
    {
        var breaker = new AICircuitBreaker();
        breaker.RecordFailure();
        breaker.RecordFailure();
        breaker.RecordFailure();

        typeof(AICircuitBreaker)
            .GetField("_openedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(breaker, DateTime.UtcNow.AddSeconds(-31));

        Assert.True(breaker.CanExecute()); // -> HALF_OPEN

        breaker.RecordFailure(); // thử vẫn lỗi
        Assert.Equal(CircuitState.Open, breaker.CurrentState); // quay lại OPEN, reset cooldown
        Assert.False(breaker.CanExecute());
    }
}