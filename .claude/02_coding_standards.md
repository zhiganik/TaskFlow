# 02 — Coding Standards & Patterns

## Language & Style

- **C# 13**, `<LangVersion>latest</LangVersion>` in `Directory.Build.props`
- **Nullable reference types enabled** — no `string?` when `string` is intended
- **Implicit usings enabled** — no boilerplate `using System;` blocks
- **Primary constructors** for simple DI injection (C# 12+):
  ```csharp
  public class TaskService(ITaskRepository repo, ICacheService cache, ILogger<TaskService> logger)
  {
      // use repo, cache, logger directly — no field assignments needed
  }
  ```
- **Collection expressions**: `List<string> items = []` not `new List<string>()`
- **Pattern matching** over type checks and casts where possible

---

## Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Classes | PascalCase | `TaskService` |
| Interfaces | `I` prefix + PascalCase | `ITaskRepository` |
| Methods | PascalCase | `GetByIdAsync` |
| Async methods | `Async` suffix | `CreateTaskAsync` |
| Private fields | `_camelCase` | `_logger` |
| Parameters / locals | camelCase | `taskId`, `ct` |
| DTOs | Noun + `Request` / `Response` / `Dto` | `CreateTaskRequest`, `TaskDto` |
| Validators | Entity + `Validator` | `CreateTaskRequestValidator` |
| Options classes | Name + `Options` | `JwtOptions`, `CacheOptions` |
| Configuration sections | Match class name | `"Jwt"` → `JwtOptions` |

---

## Controller Rules

```csharp
[ApiController]
[Route("api/v1/projects/{projectId}/tasks")]
[Authorize]
public class TasksController(ITaskService taskService) : ControllerBase
{
    /// <summary>Get paginated tasks for a project.</summary>
    [HttpGet]
    [Authorize(Policy = "WorkspaceMember")]
    [ProducesResponseType(typeof(PagedResult<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTasks(
        Guid projectId,
        [FromQuery] TaskFilter filter,
        CancellationToken ct)
    {
        var result = await taskService.GetTasksAsync(projectId, filter, ct);
        return Ok(result);
    }
}
```

**Controller checklist:**
- [ ] `[ApiController]` attribute
- [ ] `[Route("api/v1/...")]` — versioned
- [ ] `[Authorize]` at class level, specific policy at method level
- [ ] `/// <summary>` XML doc comment on every action — no `[SwaggerOperation]`. `DependencyConfig.AddSwaggerDocumentation` calls `opts.IncludeXmlComments(...)`, so the doc comment alone drives the Swagger UI title/description, and Swashbuckle tags by controller name automatically.
- [ ] `[ProducesResponseType]` for every possible status code
- [ ] No `IValidator<TRequest>` injected and no manual `ValidateAndThrowAsync` call — `AddFluentValidationAutoValidation()` (registered in `DependencyConfig`) validates the bound request automatically and short-circuits with `400` on failure
- [ ] User id read via `User.GetUserId()` (`TaskFlow.Api/Extensions/ClaimsPrincipalExtensions.cs`) — never inline `User.FindFirstValue(ClaimTypes.NameIdentifier)`
- [ ] `CancellationToken ct` on every async action
- [ ] Returns `IActionResult` (not typed `ActionResult<T>`) for consistency
- [ ] Zero try/catch blocks — GlobalExceptionMiddleware handles everything
- [ ] Zero business logic — one line: call service, return result

---

## Service Rules

```csharp
public class TaskService(
    ITaskRepository taskRepo,
    ICacheService cache,
    ILogger<TaskService> logger)
{
    public async Task<TaskDto> CreateTaskAsync(
        CreateTaskRequest request, string userId, CancellationToken ct)
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            Title = request.Title,
            Priority = request.Priority,
            Status = TaskStatus.Todo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await taskRepo.AddAsync(task, ct);

        logger.LogInformation(
            "Task {TaskId} created by {UserId} in project {ProjectId}",
            task.Id, userId, request.ProjectId);

        return task.ToDto();  // extension method or mapping method
    }
}
```

**Service checklist:**
- [ ] No EF Core, no `DbContext` — only repository interfaces
- [ ] Logs meaningful structured events (creation, updates, errors)
- [ ] Returns DTOs, never entities (entities stay in repository layer)
- [ ] Always uses `CancellationToken`
- [ ] No try/catch — exceptions bubble to GlobalExceptionMiddleware

---

## Repository Rules

```csharp
public class TaskRepository(AppDbContext db) : ITaskRepository
{
    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<PagedResult<TaskItem>> GetByProjectAsync(
        Guid projectId, TaskFilter filter, CancellationToken ct = default)
    {
        var query = db.Tasks
            .Where(t => t.ProjectId == projectId);

        if (filter.Status.HasValue)
            query = query.Where(t => t.Status == filter.Status.Value);

        if (filter.AssigneeId is not null)
            query = query.Where(t => t.AssigneeId == filter.AssigneeId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(t => t.DueDate)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return new PagedResult<TaskItem>(items, total, filter.Page, filter.PageSize);
    }
}
```

**Repository checklist:**
- [ ] `AsNoTracking()` on all read-only queries
- [ ] Build queries with LINQ, not raw SQL (unless CTE needed)
- [ ] Pagination via `Skip/Take` — never `ToList()` then filter in memory
- [ ] No business logic — only data access
- [ ] Filters applied in the DB, not after `ToList()`

---

## Configuration Rules

**Never:**
```csharp
// ❌ Magic string, not typed, not testable
var secret = _config.GetValue<string>("Jwt:Secret");
```

**Always:**
```csharp
// ✅ Typed, injected, testable
public class AuthService(IOptions<JwtOptions> jwtOptions)
{
    private readonly JwtOptions _jwt = jwtOptions.Value;
}
```

Secrets from environment variables only, bound via `PostConfigure`:
```csharp
services.PostConfigure<JwtOptions>(opts =>
    opts.Secret = Environment.GetEnvironmentVariable("JWT_SECRET")
        ?? throw new InvalidOperationException("JWT_SECRET is not set"));
```

---

## Logging Rules

```csharp
// ✅ Correct — structured properties, queryable in SEQ
_logger.LogInformation("Task {TaskId} status changed to {Status} by {UserId}",
    task.Id, newStatus, userId);

_logger.LogWarning("Cache miss for key {CacheKey} — falling back to DB", cacheKey);

_logger.LogError(ex, "Failed to assign task {TaskId} to user {AssigneeId}",
    taskId, assigneeId);

// ❌ Wrong — string interpolation loses structure
_logger.LogInformation($"Task {task.Id} updated");
```

**Log levels:**
- `Debug` — detailed internal state (dev only, filtered out in production)
- `Information` — normal operations worth knowing (created, updated, login)
- `Warning` — unexpected but non-fatal (cache miss on hot path, overdue task)
- `Error` — something failed but the app continues
- `Fatal` — startup failure only (in `Program.cs` catch block)

---

## Validation Rules

All validation in `AbstractValidator<T>` classes. Zero manual validation in controllers or services.

```csharp
public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200);

        RuleFor(x => x.Priority)
            .IsInEnum();

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.DueDate.HasValue)
            .WithMessage("Due date must be in the future.");
    }
}
```

Validation runs automatically as a filter, not explicitly in the controller: `AddFluentValidationAutoValidation()`
(registered in `DependencyConfig.AddFluentValidationServices`) finds the registered `IValidator<TRequest>` for
the action's bound parameter and runs it before the action executes, short-circuiting with `400 Bad Request`
and per-field errors when invalid. Controllers never inject `IValidator<TRequest>` and never call
`ValidateAndThrowAsync` — just declare the `AbstractValidator<T>` class and `AddValidatorsFromAssemblyContaining`
picks it up via assembly scanning. Never check `ModelState` manually in a controller.

`GlobalExceptionMiddleware` still maps `FluentValidation.ValidationException` to `400` — that path is for
validators invoked explicitly inside services for business-rule checks (e.g. file type/size), not for
request-DTO validation.

---

## Error Handling Rules

- **Zero try/catch in controllers or services**
- `GlobalExceptionMiddleware` is the single catch-all
- Returns RFC 7807 `ProblemDetails` for all errors
- Custom exception types extend `Exception` (e.g. `NotFoundException`, `ForbiddenException`) and are mapped in the middleware:

```csharp
// In GlobalExceptionMiddleware
var (status, title) = ex switch
{
    NotFoundException    => (404, "Resource not found."),
    ForbiddenException   => (403, "Access denied."),
    ConflictException    => (409, "Conflict."),
    _                    => (500, "An unexpected error occurred.")
};
```

---

## DTOs

- All inputs: `CreateXRequest`, `UpdateXRequest`, `PatchXRequest`
- All outputs: `XDto` or `XDetailDto`
- Mapping via extension methods (`task.ToDto()`) — no AutoMapper
- `PagedResult<T>` for all list endpoints:

```csharp
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
```
