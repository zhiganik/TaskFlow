# 10 — AutoMapper

## Rule
**Use AutoMapper Profiles for all entity → DTO mapping. Never add extension methods or manual mapping code outside of profiles.**

---

## Where profiles live

```
TaskFlow.Application/Mappings/
├── UserProfile.cs
├── WorkspaceProfile.cs
├── WorkspaceMemberProfile.cs
├── WorkspaceColumnProfile.cs
└── WorkspaceTaskProfile.cs
```

One profile per domain area. Each inherits `AutoMapper.Profile` and configures all maps in the constructor via `CreateMap<TSource, TDest>()`.

---

## How to add a new mapping

### Simple mapping (all property names match)
```csharp
public class MyProfile : Profile
{
    public MyProfile()
    {
        CreateMap<MyEntity, MyDto>()
            .ConstructUsing((src, ctx) => new MyDto(src.Id, src.Name, src.CreatedAt));
    }
}
```

**Always use `ConstructUsing`** — C# records have no parameterless constructor and AutoMapper 16 requires explicit constructor wiring for them.

### Computed or transformed values
Inline them in the `ConstructUsing` lambda:
```csharp
.ConstructUsing((src, ctx) => new TaskDto(
    src.Id,
    src.DueDate.HasValue && src.DueDate.Value < DateTime.UtcNow,   // computed
    src.CreatedBy?.DisplayName ?? src.CreatedById));                 // nullable fallback
```

### Runtime value not available on the source entity
Use `ctx.Items` to pass caller-supplied values. Example: `WorkspaceDto.MyRole` comes from the caller, not the entity.

**Profile:**
```csharp
CreateMap<Workspace, WorkspaceDto>()
    .ConstructUsing((src, ctx) => new WorkspaceDto(
        src.Id, src.Name, src.OwnerId, src.CreatedAt,
        (WorkspaceRole)ctx.Items["myRole"]));
```

**Call site (in service):**
```csharp
// single object
_mapper.Map<WorkspaceDto>(workspace, opts => opts.Items["myRole"] = WorkspaceRole.Owner)

// collection
memberships.Select(m =>
    _mapper.Map<WorkspaceDto>(m.Workspace, opts => opts.Items["myRole"] = m.Role))
    .ToList()
```

---

## Registration

Registered in `TaskFlow.Api/Config/DependencyConfig.cs` via the `AddMappings()` private method:
```csharp
services.AddAutoMapper(cfg =>
    cfg.AddMaps(typeof(UserProfile).Assembly));
```

`IMapper` is available for injection in all scoped services.

---

## Injecting into services

Services use primary constructor injection:
```csharp
public class MyService(IMapper mapper, ...) : IMyService
{
    public MyDto DoSomething(MyEntity entity) => _mapper.Map<MyDto>(entity);
}
```

---

## Tests

Tests use a real mapper (not a mock) so mapping logic is verified alongside service logic:
```csharp
private IMapper _mapper = null!;

[SetUp]
public void SetUp()
{
    _mapper = new ServiceCollection()
        .AddLogging()
        .AddAutoMapper(cfg => cfg.AddProfile<MyProfile>())
        .BuildServiceProvider()
        .GetRequiredService<IMapper>();

    _sut = new MyService(_repositoryMock.Object, _mapper, _loggerMock.Object);
}
```

AutoMapper 16 requires `AddLogging()` to be registered before `AddAutoMapper` in tests.
