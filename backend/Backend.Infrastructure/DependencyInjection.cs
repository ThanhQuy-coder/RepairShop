using System.Text;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Identity;
using RepairShop.Infrastructure.Identity;
using RepairShop.Infrastructure.Persistence;
using RepairShop.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RepairShop.Domain.Common;
using RepairShop.Infrastructure.ExternalServices;

namespace RepairShop.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' không tìm thấy trong appsettings.json");

        services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                    npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));

                options.EnableDetailedErrors();

                options.EnableSensitiveDataLogging();

                options.LogTo(
                    Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
            }
        );

        services.AddHttpContextAccessor();

        // Authentication
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IRepairTicketRepository, RepairTicketRepository>();
        services.AddScoped<IRepairStatusRepository, RepairStatusRepository>();
        services.AddScoped<ITicketCodeGenerator, TicketCodeGenerator>();
        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddScoped<IPartRepository, PartRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
           ?? throw new InvalidOperationException("Cấu hình Jwt không tìm thấy trong appsettings.");
        var jwtSecret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret chưa được cấu hình.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
                policy.RequireRole(Roles.Admin));

            options.AddPolicy(AuthorizationPolicies.StaffOnly, policy =>
                policy.RequireRole(Roles.Admin, Roles.Receptionist, Roles.Technician));

            options.AddPolicy(AuthorizationPolicies.ReceptionistOrAdmin, policy =>
                policy.RequireRole(Roles.Receptionist, Roles.Admin));

            options.AddPolicy(AuthorizationPolicies.TechnicianOrAdmin, policy =>
                policy.RequireRole(Roles.Technician, Roles.Admin));

            options.AddPolicy(AuthorizationPolicies.InventoryViewers, policy =>
                policy.RequireRole(Roles.Technician, Roles.Admin));
        });

        services.Configure<CloudinarySettings>(configuration.GetSection(CloudinarySettings.SectionName));
        services.AddScoped<IFileStorageService, CloudinaryFileStorageService>();

        return services;
    }
}