# 01 — Architecture & Layer Rules

## Solution Structure

```
TaskFlow/
├── TaskFlow.Api/            # HTTP layer only
├── TaskFlow.Application/    # Business logic, no infrastructure
├── TaskFlow.Infrastructure/ # EF Core, Redis, external services
└── TaskFlow.Tests/          # NUnit unit tests
```

### Dependency Direction (strict)
```
Api  →  Application  →  (interfaces only)
Infrastructure  →  Application  →  (implements interfaces)
Tests  →  Application  (tests Application through mocked interfaces)
```
**Infrastructure never references Api. Api never references Infrastructure directly.**

---

## Program.cs — Exact Pattern

```csharp
// Bootstrap logger first — catches startup exceptions
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting TaskFlow API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddDependencies(builder.Configuration);
    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext()
           .WriteTo.Console(new JsonFormatter()));

    var app = builder.Build();

    app.UseApplicationPipeline();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

Never add logic directly to Program.cs. Every registration goes in `DependencyConfig`. Every middleware call goes in `ApplicationConfig`.

---

## DependencyConfig.cs

**File:** `TaskFlow.Api/Config/DependencyConfig.cs`

```csharp
public static class DependencyConfig
{
    public static IServiceCollection AddDependencies(
        this IServiceCollection services, IConfiguration config)
    {
        services
            .AddOptions(config)
            .AddDatabase(config)
            .AddRedisCache(config)
            .AddRepositories()
            .AddApplicationServices()
            .AddJwtAuth(config)
            .AddFluentValidationServices()
            .AddSwaggerDocumentation()
            .AddControllers();
        return services;
    }
}
```

Each `Add*` is a private static method in the same file. Group related registrations together.

---

## ApplicationConfig.cs

**File:** `TaskFlow.Api/Config/ApplicationConfig.cs`

```csharp
public static WebApplication UseApplicationPipeline(this WebApplication app)
{
    app.UseMiddleware<GlobalExceptionMiddleware>();  // ALWAYS first
    app.UseMiddleware<RequestLoggingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(opts =>
        {
            opts.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskFlow API v1");
            opts.DisplayRequestDuration();
        });
    }

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    return app;
}
```

**Middleware order is critical:**
1. GlobalExceptionMiddleware (must catch everything)
2. RequestLoggingMiddleware
3. UseAuthentication
4. UseAuthorization
5. MapControllers

---

## Layer Responsibilities

### Api layer
- Controllers (thin — delegate immediately to service; validate via injected `IValidator<TRequest>` first)
- Middleware (`GlobalExceptionMiddleware`, `RequestLoggingMiddleware`)
- BackgroundServices (`DeadlineNotifierService`)
- Config (`DependencyConfig`, `ApplicationConfig`)

### Application layer
- Interfaces (`ITaskRepository`, `ICacheService`, etc.)
- Services (`TaskService`, `ProjectService`, `WorkspaceService`)
- DTOs (request/response objects, `PagedResult<T>`)
- Validators (FluentValidation `AbstractValidator<T>`)
- Options classes (`JwtOptions`, `CacheOptions`)

### Infrastructure layer
- `AppDbContext` (inherits `IdentityDbContext<AppUser>`)
- Entity configurations (`IEntityTypeConfiguration<T>`)
- Repository implementations
- `RedisCacheService`
- Migrations (generated, never hand-edited)

---

## DI Lifetimes

| Type | Lifetime | Why |
|------|----------|-----|
| `AppDbContext` | Scoped | One DB connection per HTTP request |
| `IRepository` | Scoped | Uses DbContext |
| `ICacheService` | Singleton | Redis connection is thread-safe and expensive to create |
| `ITaskService` | Scoped | Uses repositories |
| `DeadlineNotifierService` | Singleton (IHostedService) | Use `IServiceScopeFactory` to resolve scoped deps |
| `IOptions<T>` | Singleton | Immutable config |

**BackgroundService rule:** Never inject scoped services directly. Always inject `IServiceScopeFactory` and create a scope inside `ExecuteAsync`.
