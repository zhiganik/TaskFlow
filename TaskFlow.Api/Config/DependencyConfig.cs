using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TaskFlow.Infrastructure.Messaging;
using TaskFlow.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using TaskFlow.Api.Authorization;
using TaskFlow.Api.Swagger;
using TaskFlow.Application.Domain.Constants;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Application.Options;
using TaskFlow.Application.Services;
using TaskFlow.Application.Validators;
using TaskFlow.Infrastructure.HealthChecks;
using TaskFlow.Infrastructure.Caching;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Repositories;

namespace TaskFlow.Api.Config;

public static class DependencyConfig
{
    public static IServiceCollection AddDependencies(
        this IServiceCollection services, IConfiguration config)
    {
        services
            .AddOptions(config)
            .AddDatabase()
            .AddRedisCache()
            .AddIdentityServices()
            .AddJwtAuthentication(config)
            .AddRepositories()
            .AddApplicationServices()
            .AddMappings()
            .AddCors()
            .AddFluentValidationServices()
            .AddSwaggerDocumentation()
            .AddMessageBus()
            .AddInfrastructureHealthChecks();

        services.AddControllers()
            .AddJsonOptions(opts => opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        return services;
    }
    
    private static IServiceCollection AddCors(this IServiceCollection services)
    {
        var origins = Environment.GetEnvironmentVariable("CORS_ORIGINS")
                      ?? "http://localhost:3000";

        services.AddCors(opts =>
            opts.AddPolicy("AllowFrontend", policy =>
                policy.WithOrigins(origins.Split(','))
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()));

        return services;
    }

    private static IServiceCollection AddOptions(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<JwtOptions>(config.GetSection("Jwt"));
        services.PostConfigure<JwtOptions>(opts =>
            opts.Secret = Environment.GetEnvironmentVariable("JWT_SECRET")
                ?? throw new InvalidOperationException("JWT_SECRET env var is required"));

        services.Configure<CacheOptions>(config.GetSection("Cache"));
        services.Configure<StorageOptions>(config.GetSection("Storage"));
        services.Configure<S3Options>(opts =>
        {
            opts.BucketName = Environment.GetEnvironmentVariable("S3_BUCKET_NAME") ?? string.Empty;
            opts.ServiceUrl = Environment.GetEnvironmentVariable("S3_SERVICE_URL") ?? string.Empty;
            opts.AccessKey  = Environment.GetEnvironmentVariable("S3_ACCESS_KEY")  ?? string.Empty;
            opts.SecretKey  = Environment.GetEnvironmentVariable("S3_SECRET_KEY")  ?? string.Empty;
            opts.Region     = Environment.GetEnvironmentVariable("S3_REGION")      ?? "auto";
        });

        services.Configure<AppOptions>(opts =>
        {
            opts.FrontendBaseUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? "http://localhost:3000";
        });

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services)
    {
        var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
            ?? throw new InvalidOperationException("POSTGRES_CONNECTION env var is required");

        services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(connectionString));

        return services;
    }

    private static IServiceCollection AddRedisCache(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var connectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION")
                ?? throw new InvalidOperationException("REDIS_CONNECTION env var is required");
            return ConnectionMultiplexer.Connect(connectionString);
        });

        // RedisCacheService registered as concrete so HybridCacheService can inject it directly.
        services.AddSingleton<RedisCacheService>();
        // HybridCacheService (L1 IMemoryCache + L2 Redis) is the default ICacheService.
        services.AddSingleton<ICacheService, HybridCacheService>();

        return services;
    }

    private static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }

    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            var jwt = config.GetSection("Jwt").Get<JwtOptions>()
                ?? throw new InvalidOperationException("Jwt configuration section is missing.");

            var secret = Environment.GetEnvironmentVariable("JWT_SECRET")
                ?? throw new InvalidOperationException("JWT_SECRET env var is required");

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
            };
        });

        services.AddAuthorization(opts =>
        {
            opts.AddPolicy(WorkspacePolicies.Member,
                p => p.AddRequirements(new WorkspaceRoleRequirement(WorkspaceRole.Member)));
            opts.AddPolicy(WorkspacePolicies.Admin,
                p => p.AddRequirements(new WorkspaceRoleRequirement(WorkspaceRole.Admin)));
            opts.AddPolicy(WorkspacePolicies.Owner,
                p => p.AddRequirements(new WorkspaceRoleRequirement(WorkspaceRole.Owner)));
        });

        services.AddScoped<IAuthorizationHandler, WorkspaceRoleHandler>();

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IWorkspacesRepository, WorkspacesRepository>();
        services.AddScoped<IWorkspaceMembersRepository, WorkspaceMembersRepository>();
        services.AddScoped<IWorkspaceInvitationRepository, WorkspaceInvitationRepository>();
        services.AddScoped<IWorkspaceColumnsRepository, WorkspaceColumnsRepository>();
        services.AddScoped<IWorkspaceTasksRepository, WorkspaceTasksRepository>();
        services.AddScoped<ITaskCommentsRepository, TaskCommentsRepository>();
        services.AddScoped<IWorkspaceLabelsRepository, WorkspaceLabelsRepository>();
        services.AddScoped<IWorkspacePriorityConfigRepository, WorkspacePriorityConfigRepository>();
        services.AddScoped<ITaskAttachmentRepository, TaskAttachmentRepository>();
        services.AddScoped<IArchiveRepository, ArchiveRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IWorkspacesService, WorkspacesService>();
        services.AddScoped<IWorkspaceMembersService, WorkspaceMembersService>();
        services.AddScoped<IWorkspaceColumnsService, WorkspaceColumnsService>();
        services.AddScoped<IWorkspaceTasksService, WorkspaceTasksService>();
        services.AddScoped<ITaskCommentsService, TaskCommentsService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IWorkspaceLabelsService, WorkspaceLabelsService>();
        services.AddScoped<IWorkspacePriorityConfigService, WorkspacePriorityConfigService>();
        services.AddScoped<ITaskAttachmentService, TaskAttachmentService>();
        services.AddScoped<IArchiveService, ArchiveService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IWorkspaceInvitationService, WorkspaceInvitationService>();
        services.AddScoped<IDemoWorkspaceSeeder, DemoWorkspaceSeeder>();
        return services;
    }

    private static IServiceCollection AddMappings(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg =>
            cfg.AddMaps(typeof(TaskFlow.Application.Mappings.UserProfile).Assembly));
        return services;
    }

    private static IServiceCollection AddFluentValidationServices(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        return services;
    }

    private static IServiceCollection AddMessageBus(this IServiceCollection services)
    {
        var storageType = Environment.GetEnvironmentVariable("STORAGE_TYPE") ?? "local";
        if (storageType == "s3")
            services.AddSingleton<IBlobService, S3BlobService>();
        else
            services.AddSingleton<IBlobService, LocalFileBlobService>();
        services.AddSingleton<ITemporaryFileStore, RedisTemporaryFileStore>();
        services.AddScoped<IMessagePublisher, MassTransitPublisher>();

        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((_, cfg) =>
            {
                var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST")
                    ?? throw new InvalidOperationException("RABBITMQ_HOST env var is required");
                var user = Environment.GetEnvironmentVariable("RABBITMQ_USER")
                    ?? throw new InvalidOperationException("RABBITMQ_USER env var is required");
                var pass = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")
                    ?? throw new InvalidOperationException("RABBITMQ_PASSWORD env var is required");

                cfg.Host(host, "/", h =>
                {
                    h.Username(user);
                    h.Password(pass);
                });
            });
        });

        return services;
    }

    private static IServiceCollection AddInfrastructureHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks().AddInfrastructureChecks();
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
            opts.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));

            opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token"
            });
            opts.OperationFilter<AuthorizeOperationFilter>();
        });
        return services;
    }
}
