using Backend.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Đăng ký Behavior dạng generic mở — áp dụng cho MỌI Command/Query, không cần khai từng cái
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

        return services;
    }
}