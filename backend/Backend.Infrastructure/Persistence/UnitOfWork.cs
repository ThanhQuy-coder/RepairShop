using RepairShop.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace RepairShop.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context) => _context = context;

    /// <summary>
    /// Dùng ExecutionStrategy (không tự BeginTransaction tay) vì Npgsql provider có retry logic
    /// riêng cho lỗi tạm thời (transient failure) — tự quản lý transaction sẽ xung đột với
    /// cơ chế retry đó nếu không dùng đúng API này.
    /// </summary>
    public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await operation();
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw; // ném lại nguyên vẹn để ExceptionHandlingMiddleware (Task 8) xử lý đúng loại lỗi
            }
        });
    }
}