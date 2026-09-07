namespace RepairShop.Infrastructure.AI;

public interface IAICircuitBreaker
{
    bool CanExecute();
    void RecordSuccess();
    void RecordFailure();
}