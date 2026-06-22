using Microsoft.OpenApi.Models;
using TaskFlow.Application.Interfaces;
using TaskFlow.Application.Services;

namespace TaskFlow.Api.Config;

public static class DependencyConfig
{
    public static IServiceCollection AddDependencies(
        this IServiceCollection services, IConfiguration config)
    {
        services
            .AddApplicationServices()
            .AddSwaggerDocumentation()
            .AddControllers();
        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IHealthService, HealthService>();
        return services;
    }

    private static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(opts =>
        {
            opts.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "TaskFlow API",
                Version = "v1",
                Description = "Task & Project Management REST API"
            });
            opts.EnableAnnotations();
        });
        return services;
    }
}
