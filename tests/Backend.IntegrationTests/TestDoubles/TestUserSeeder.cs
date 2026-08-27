using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Common;
using RepairShop.Domain.Modules.Identity;
using RepairShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace RepairShop.IntegrationTests.TestDoubles;

/// <summary>
/// Seed trực tiếp qua DbContext (bỏ qua HTTP) vì UsersController.Create (Task 10) vẫn còn là khung
/// rỗng — Register công khai (Task 6) chỉ tạo được Role Customer. Đây là cách hợp lệ để chuẩn bị
/// dữ liệu Arrange cho Integration Test mà không phụ thuộc vào 1 API chưa hoàn thiện.
/// </summary>
public static class TestUserSeeder
{
    public record SeededUser(Guid UserId, string Email, string Token);

    public static async Task<SeededUser> SeedUserAsync(
        IServiceProvider services, string roleName, string emailPrefix)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var jwtGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

        var role = await db.Roles.FirstAsync(r => r.Name == roleName);
        var email = $"{emailPrefix}-{Guid.NewGuid():N}@test.local";

        var user = new User($"Test {roleName} {emailPrefix}", email, hasher.Hash("Test@123456"), role.Id);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        if (roleName == Roles.Customer)
        {
            var customer = new Domain.Modules.Customers.Customer(
                user.FullName, $"09{Random.Shared.Next(10000000, 99999999)}", email, null, user.Id);
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
        }

        var token = jwtGenerator.GenerateAccessToken(user, roleName);
        return new SeededUser(user.Id, email, token);
    }

    public static async Task<Guid> SeedPartWithStockAsync(IServiceProvider services, int quantity)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var part = new Domain.Modules.Inventory.Part("Pin iPhone 13", $"SKU-{Guid.NewGuid():N}"[..12], 350000);
        db.Parts.Add(part);

        var inventory = new Domain.Modules.Inventory.Inventory(part.Id);
        inventory.Add(quantity);
        db.Inventories.Add(inventory);

        await db.SaveChangesAsync();
        return part.Id;
    }
}