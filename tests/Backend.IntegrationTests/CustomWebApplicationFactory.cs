using RepairShop.Application.Common.Interfaces;
using RepairShop.Infrastructure.Persistence;
using RepairShop.IntegrationTests.TestDoubles;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace RepairShop.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("repairshop_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Gỡ AppDbContext đang trỏ appsettings.json thật, thay bằng connection string của container test
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_dbContainer.GetConnectionString()));

            // Thay Cloudinary thật bằng Fake — không phụ thuộc dịch vụ ngoài khi test
            var storageDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IFileStorageService));
            if (storageDescriptor is not null) services.Remove(storageDescriptor);
            services.AddScoped<IFileStorageService, FakeFileStorageService>();
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        // Chạy migration thật (bao gồm seed Role/RepairStatus ở Bước 0) trên DB container mới tinh
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync() => await _dbContainer.StopAsync();
}