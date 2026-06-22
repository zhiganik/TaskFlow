# 04 — Auth: ASP.NET Identity + JWT + RBAC

## Overview

- **ASP.NET Identity** manages users, passwords (PBKDF2), lockout, tokens
- **JWT** is issued on login and attached to all subsequent requests
- **RBAC** is enforced via policy-based authorization at the controller level
- **Workspace role** (Owner/Admin/Member) is NOT embedded in JWT — resolved per-request from DB

---

## AppUser

```csharp
public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public ICollection<WorkspaceMember> WorkspaceMemberships { get; set; } = [];
}
```

`IdentityUser` provides: `Id` (string GUID), `Email`, `UserName`, `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, `LockoutEnd`, `AccessFailedCount`, etc.

---

## Identity Registration

```csharp
// Inside DependencyConfig.AddDatabase()
services.AddIdentity<AppUser, IdentityRole>(opts =>
{
    opts.Password.RequiredLength = 8;
    opts.Password.RequireNonAlphanumeric = true;
    opts.Password.RequireUppercase = true;
    opts.User.RequireUniqueEmail = true;
    opts.Lockout.MaxFailedAccessAttempts = 5;
    opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();
```

---

## JWT Configuration

### JwtOptions
```csharp
// TaskFlow.Application/Options/JwtOptions.cs
public class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
    public string Secret { get; set; } = string.Empty;  // from env var, never appsettings
}
```

### Registration
```csharp
// In DependencyConfig.AddJwtAuth()
services.Configure<JwtOptions>(config.GetSection("Jwt"));
services.PostConfigure<JwtOptions>(opts =>
    opts.Secret = Environment.GetEnvironmentVariable("JWT_SECRET")
        ?? throw new InvalidOperationException("JWT_SECRET env var is required"));

services.AddAuthentication(opts =>
{
    opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opts =>
{
    var jwt = config.GetSection("Jwt").Get<JwtOptions>()!;
    opts.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt.Issuer,
        ValidAudience = jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                Environment.GetEnvironmentVariable("JWT_SECRET")!))
    };
});
```

### Token Generation
```csharp
// In AuthService
public string GenerateToken(AppUser user)
{
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Email, user.Email!),
        new Claim("displayName", user.DisplayName),
    };

    var key = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(_jwt.Secret));

    var token = new JwtSecurityToken(
        issuer: _jwt.Issuer,
        audience: _jwt.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes),
        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

---

## RBAC — Policy-Based Authorization

### Policy Registration
```csharp
// In DependencyConfig.AddJwtAuth()
services.AddAuthorization(opts =>
{
    opts.AddPolicy("WorkspaceMember",
        p => p.AddRequirements(new WorkspaceRoleRequirement(WorkspaceRole.Member)));
    opts.AddPolicy("WorkspaceAdmin",
        p => p.AddRequirements(new WorkspaceRoleRequirement(WorkspaceRole.Admin)));
    opts.AddPolicy("WorkspaceOwner",
        p => p.AddRequirements(new WorkspaceRoleRequirement(WorkspaceRole.Owner)));
});

services.AddScoped<IAuthorizationHandler, WorkspaceRoleHandler>();
```

### Requirement
```csharp
public class WorkspaceRoleRequirement(WorkspaceRole minimumRole)
    : IAuthorizationRequirement
{
    public WorkspaceRole MinimumRole { get; } = minimumRole;
}
```

### Handler
```csharp
public class WorkspaceRoleHandler(IWorkspaceRepository workspaceRepo)
    : AuthorizationHandler<WorkspaceRoleRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext ctx,
        WorkspaceRoleRequirement requirement)
    {
        // Get workspaceId from route — works for nested routes too
        if (ctx.Resource is not HttpContext httpContext)
            return;

        var routeValues = httpContext.GetRouteData().Values;

        if (!routeValues.TryGetValue("workspaceId", out var wsIdObj)
            || !Guid.TryParse(wsIdObj?.ToString(), out var workspaceId))
            return;

        var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return;

        // Uses composite (WorkspaceId, UserId) index — O(1) lookup
        var member = await workspaceRepo.GetMemberAsync(workspaceId, userId);
        if (member is null) return;

        // Role hierarchy: Owner > Admin > Member
        if (member.Role <= requirement.MinimumRole)
            ctx.Succeed(requirement);
    }
}
```

### Usage in Controllers
```csharp
[HttpPost]
[Authorize(Policy = "WorkspaceAdmin")]  // Admin or Owner can create
public async Task<IActionResult> CreateProject(...) { }

[HttpGet]
[Authorize(Policy = "WorkspaceMember")]  // Any member can read
public async Task<IActionResult> GetProjects(...) { }
```

---

## Reading User Identity in Services

Never pass the full `ClaimsPrincipal` into services. Extract the user ID in the controller and pass it as a parameter:

```csharp
// In controller
var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
var result = await taskService.CreateTaskAsync(request, userId, ct);
```

Services receive `string userId`, not `ClaimsPrincipal`. This keeps services testable without mocking HTTP context.

---

## JWT Claims

```json
{
  "sub": "aspnet-identity-user-id-string",
  "email": "user@example.com",
  "displayName": "Jane Smith",
  "iat": 1700000000,
  "exp": 1700003600,
  "iss": "taskflow-api",
  "aud": "taskflow-clients"
}
```

Workspace role is NOT in the token. It is resolved per-request by `WorkspaceRoleHandler` using the DB (composite index). This ensures role changes take effect immediately without token reissue.
