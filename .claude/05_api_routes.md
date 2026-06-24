# 05 — API Routes, DTOs & Swagger

## Routing Convention

```
/api/v1/auth                                         → AuthController
/api/v1/workspaces                                   → WorkspacesController
/api/v1/workspaces/{workspaceId}/members             → WorkspaceMembersController
/api/v1/workspaces/{workspaceId}/projects            → ProjectsController
/api/v1/projects/{projectId}/tasks                   → TasksController
/api/v1/tasks/{taskId}/attachments                   → TaskAttachmentsController  ← NEW
/api/v1/me/tasks                                     → MeController
```

---

## Swagger Annotation Pattern

Every action MUST have:

```csharp
/// <summary>One line — appears as the route title in Swagger UI.</summary>
[HttpVerb("route")]
[Authorize(Policy = "PolicyName")]
[ProducesResponseType(typeof(ResponseDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
```

No `[SwaggerOperation]` — the `/// <summary>` XML doc comment is enough. `DependencyConfig.AddSwaggerDocumentation`
calls `opts.IncludeXmlComments(...)`, so Swashbuckle reads the summary directly, and it tags operations by
controller name automatically. The `Swashbuckle.AspNetCore.Annotations` package is not referenced.

File upload endpoints use `[Consumes("multipart/form-data")]`.

---

## Route Table

### Auth — no auth required
| Method | Route | Returns |
|--------|-------|---------|
| POST | `/auth/register` | `201 UserDto` / `400` |
| POST | `/auth/login` | `200 AuthResponseDto` / `400` / `401` |

### Workspaces — 🔒
| Method | Route | Policy | Returns |
|--------|-------|--------|---------|
| POST | `/workspaces` | Authenticated | `201 WorkspaceDto` |
| GET | `/workspaces` | Authenticated | `200 WorkspaceDto[]` |
| GET | `/workspaces/{id}` | Member | `200 WorkspaceDto` (cached) |
| PUT | `/workspaces/{id}` | Owner | `200 WorkspaceDto` |
| DELETE | `/workspaces/{id}` | Owner | `204` |
| GET | `/workspaces/{id}/members` | Member | `200 MemberDto[]` |
| POST | `/workspaces/{id}/members` | Admin | `201 MemberDto` |
| PUT | `/workspaces/{id}/members/{userId}` | Admin | `200 MemberDto` |
| DELETE | `/workspaces/{id}/members/{userId}` | Admin | `204` |

### Projects — 🔒
| Method | Route | Policy | Returns |
|--------|-------|--------|---------|
| GET | `/workspaces/{id}/projects` | Member | `200 ProjectDto[]` (cached) |
| POST | `/workspaces/{id}/projects` | Admin | `201 ProjectDto` |
| GET | `/workspaces/{id}/projects/{projectId}` | Member | `200 ProjectDetailDto` |
| PUT | `/workspaces/{id}/projects/{projectId}` | Admin | `200 ProjectDto` |
| DELETE | `/workspaces/{id}/projects/{projectId}` | Admin | `204` |

### Tasks — 🔒
| Method | Route | Policy | Returns |
|--------|-------|--------|---------|
| GET | `/projects/{projectId}/tasks` | Member | `200 PagedResult<TaskDto>` |
| POST | `/projects/{projectId}/tasks` | Member | `201 TaskDto` |
| GET | `/projects/{projectId}/tasks/{id}` | Member | `200 TaskDto` (with attachments) |
| PUT | `/projects/{projectId}/tasks/{id}` | Member | `200 TaskDto` |
| PATCH | `/projects/{projectId}/tasks/{id}/status` | Member | `200 TaskDto` |
| PATCH | `/projects/{projectId}/tasks/{id}/assign` | Admin | `200 TaskDto` |
| DELETE | `/projects/{projectId}/tasks/{id}` | Admin | `204` |

### Task Attachments — 🔒 *(new)*
| Method | Route | Policy | Returns |
|--------|-------|--------|---------|
| POST | `/tasks/{taskId}/attachments` | Member | `202 AttachmentDto` |
| GET | `/tasks/{taskId}/attachments` | Member | `200 AttachmentDto[]` |
| GET | `/tasks/{taskId}/attachments/{id}` | Member | `200 AttachmentDto` |
| GET | `/tasks/{taskId}/attachments/{id}/download` | Member | `200 FileStreamResult` |
| DELETE | `/tasks/{taskId}/attachments/{id}` | Admin | `204` |

### Me — 🔒
| Method | Route | Returns |
|--------|-------|---------|
| GET | `/me/tasks` | `200 PagedResult<TaskDto>` |

---

## DTO Reference

### Request DTOs
```csharp
public record RegisterRequest(string Email, string DisplayName, string Password);
public record LoginRequest(string Email, string Password);

public record CreateWorkspaceRequest(string Name);
public record UpdateWorkspaceRequest(string Name);

public record InviteMemberRequest(string Email, WorkspaceRole Role);
public record UpdateMemberRoleRequest(WorkspaceRole Role);

public record CreateProjectRequest(string Name, string? Description);
public record UpdateProjectRequest(string Name, string? Description);

public record CreateTaskRequest(
    string Title, string? Description,
    TaskPriority Priority, string? AssigneeId, DateTime? DueDate);

public record UpdateTaskRequest(
    string Title, string? Description,
    TaskPriority Priority, DateTime? DueDate);

public record PatchTaskStatusRequest(TaskStatus Status);
public record PatchTaskAssignRequest(string? AssigneeId);

// Attachment upload uses IFormFile directly — no request record
// Bound in controller as: [FromForm] IFormFile file
```

### Response DTOs
```csharp
public record UserDto(string UserId, string Email, string DisplayName);
public record AuthResponseDto(string AccessToken, DateTime ExpiresAt, UserDto User);
public record WorkspaceDto(Guid Id, string Name, string OwnerId, DateTime CreatedAt);
public record MemberDto(string UserId, string DisplayName, string Email, WorkspaceRole Role);
public record ProjectDto(Guid Id, Guid WorkspaceId, string Name, string? Description, DateTime CreatedAt);
public record TaskSummaryDto(int Total, int Todo, int InProgress, int Done, int Cancelled);
public record ProjectDetailDto(
    Guid Id, Guid WorkspaceId, string Name, string? Description,
    DateTime CreatedAt, TaskSummaryDto TaskSummary);

// Attachments included on single-task GET; empty list on paginated list
public record TaskDto(
    Guid Id, Guid ProjectId, string Title, string? Description,
    TaskPriority Priority, TaskStatus Status, DateTime? DueDate,
    DateTime CreatedAt, DateTime UpdatedAt,
    UserDto? Assignee,
    IReadOnlyList<AttachmentDto> Attachments);  // ← updated

public record AttachmentDto(                    // ← new
    Guid Id,
    Guid TaskId,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    AttachmentStatus Status,
    string? ProcessingError,
    DateTime UploadedAt,
    DateTime? ProcessedAt,
    UserDto UploadedBy);

public record PagedResult<T>(
    IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
```

---

## Attachment Controller Pattern

```csharp
[ApiController]
[Route("api/v1/tasks/{taskId}/attachments")]
[Authorize]
public class TaskAttachmentsController(ITaskAttachmentService attachmentService)
    : ControllerBase
{
    /// <summary>Upload a file to a task. Queued for background processing.</summary>
    [HttpPost]
    [Authorize(Policy = "WorkspaceMember")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AttachmentDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [RequestSizeLimit(20 * 1024 * 1024)]  // 20 MB
    public async Task<IActionResult> Upload(
        Guid taskId,
        IFormFile file,
        CancellationToken ct)
    {
        var dto = await attachmentService.UploadAsync(taskId, file, User.GetUserId(), ct);
        return AcceptedAtAction(nameof(GetById), new { taskId, id = dto.Id }, dto);
    }

    /// <summary>Download attachment file. Only available when status is Ready.</summary>
    [HttpGet("{id}/download")]
    [Authorize(Policy = "WorkspaceMember")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid taskId, Guid id, CancellationToken ct)
    {
        var (stream, fileName, contentType) =
            await attachmentService.DownloadAsync(taskId, id, ct);

        return File(stream, contentType,
            fileDownloadName: fileName,
            enableRangeProcessing: true);
    }
}
```

---

## StorageOptions (new IOptions section)

```csharp
// TaskFlow.Application/Options/StorageOptions.cs
public class StorageOptions
{
    public string BasePath { get; set; } = string.Empty;      // absolute path on disk
    public long MaxFileSizeBytes { get; set; } = 20_971_520;  // 20 MB default
    public string[] AllowedExtensions { get; set; } =
        [".pdf", ".png", ".jpg", ".jpeg", ".gif", ".txt", ".docx", ".xlsx"];
}
```

```json
// appsettings.json (safe values — no secrets)
"Storage": {
  "BasePath": "/app/uploads",
  "MaxFileSizeBytes": 20971520,
  "AllowedExtensions": [".pdf", ".png", ".jpg", ".jpeg", ".gif", ".txt", ".docx", ".xlsx"]
}
```

---

## HTTP Status Code Conventions

| Code | When |
|------|------|
| `200` | Read or update succeeded |
| `201` | Resource created — include `Location` header |
| `202` | File uploaded, queued for async processing |
| `204` | Delete succeeded — no body |
| `400` | Validation / business rule (file too large, bad type, attachment not ready) |
| `401` | Missing or invalid JWT |
| `403` | Valid JWT but insufficient role |
| `404` | Resource not found |
| `409` | Conflict |
| `500` | Unhandled exception — logged, generic ProblemDetails |
