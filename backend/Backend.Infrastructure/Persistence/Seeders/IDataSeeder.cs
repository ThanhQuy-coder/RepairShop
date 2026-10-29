namespace RepairShop.Infrastructure.Persistence.Seeders;

public interface IDataSeeder
{
    int Order { get; }

    Task SeedAsync(CancellationToken cancellationToken = default);
}