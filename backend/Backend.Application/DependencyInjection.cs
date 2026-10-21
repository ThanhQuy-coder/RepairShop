using RepairShop.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Auth;

namespace RepairShop.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Đăng ký MediatR với DI Container
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // Đăng ký tất cả các luật kiểm tra (Validators) của FluentValidation
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Kích hoạt Validation Pipeline Behavior cho MediatR
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddScoped<IOtpGenerator, OtpGenerator>();

        return services;
    }
}