namespace RepairShop.Infrastructure.AI;

public enum CircuitState { Closed, Open, HalfOpen }

/// <summary>
/// Basic Circuit Breaker — KHÔNG dùng thư viện ngoài (Polly), tự viết đơn giản đúng tinh thần
/// "không cần làm quá phức tạp" mentor lưu ý. Đăng ký Singleton vì trạng thái (đếm lỗi liên tiếp,
/// thời điểm mở circuit) PHẢI dùng chung xuyên suốt vòng đời ứng dụng, không phải theo từng request.
///
/// State machine:
/// CLOSED (bình thường) --[đủ N lỗi liên tiếp]--> OPEN (chặn gọi AI)
/// OPEN --[đã qua thời gian cooldown]--> HALF_OPEN (cho thử 1 request)
/// HALF_OPEN --[thành công]--> CLOSED | HALF_OPEN --[thất bại]--> OPEN (reset lại cooldown)
/// </summary>
public class AICircuitBreaker : IAICircuitBreaker
{
    private const int FailureThreshold = 3;              // 3 lỗi 503 liên tiếp -> mở circuit
    private static readonly TimeSpan CooldownPeriod = TimeSpan.FromSeconds(30); // đủ ngắn để demo, đủ dài để tránh spam

    private readonly object _lock = new();
    private CircuitState _state = CircuitState.Closed;
    private int _consecutiveFailures;
    private DateTime _openedAt = DateTime.MinValue;

    public bool CanExecute()
    {
        lock (_lock)
        {
            switch (_state)
            {
                case CircuitState.Closed:
                    return true;

                case CircuitState.Open:
                    if (DateTime.UtcNow - _openedAt >= CooldownPeriod)
                    {
                        // Hết thời gian cooldown -> chuyển HALF_OPEN, cho phép 1 request thử nghiệm
                        _state = CircuitState.HalfOpen;
                        return true;
                    }
                    return false; // vẫn trong thời gian OPEN -> chặn, không lãng phí request

                case CircuitState.HalfOpen:
                    // Chỉ cho 1 request "thăm dò" tại 1 thời điểm — các request khác trong lúc
                    // HALF_OPEN vẫn bị chặn cho tới khi có kết quả của request thăm dò đó.
                    return true;

                default:
                    return true;
            }
        }
    }

    public void RecordSuccess()
    {
        lock (_lock)
        {
            _consecutiveFailures = 0;
            _state = CircuitState.Closed; // HALF_OPEN thành công -> đóng lại hoàn toàn
        }
    }

    public void RecordFailure()
    {
        lock (_lock)
        {
            _consecutiveFailures++;

            if (_state == CircuitState.HalfOpen)
            {
                // Thử nghiệm ở HALF_OPEN vẫn thất bại -> quay lại OPEN, reset thời gian cooldown
                _state = CircuitState.Open;
                _openedAt = DateTime.UtcNow;
                return;
            }

            if (_consecutiveFailures >= FailureThreshold && _state == CircuitState.Closed)
            {
                _state = CircuitState.Open;
                _openedAt = DateTime.UtcNow;
            }
        }
    }

    // Expose để test/monitoring — không phải phần bắt buộc của interface
    public CircuitState CurrentState { get { lock (_lock) { return _state; } } }
}