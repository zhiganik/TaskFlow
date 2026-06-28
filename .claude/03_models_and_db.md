# 03 — Models, EF Core & Database

## Entities

### AppUser (extends IdentityUser)
```csharp
public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ICollection<WorkspaceMember> WorkspaceMemberships { get; set; } = [];
}
```

### Workspace
```csharp
public class Workspace
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;  // string FK → AppUser.Id
    public DateTime CreatedAt { get; set; }

    public AppUser Owner { get; set; } = null!;
    public ICollection<WorkspaceMember> Members { get; set; } = [];
    public ICollection<Project> Projects { get; set; } = [];
}
```

### WorkspaceMember
```csharp
public class WorkspaceMember
{
    public Guid WorkspaceId { get; set; }
    public string UserId { get; set; } = string.Empty;  // string FK → AppUser.Id
    public WorkspaceRole Role { get; set; }
    public DateTime JoinedAt { get; set; }

    public Workspace Workspace { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}

public enum WorkspaceRole { Owner = 0, Admin = 1, Member = 2 }
```

### Project
```csharp
public class Project
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public Workspace Workspace { get; set; } = null!;
    public ICollection<TaskItem> Tasks { get; set; } = [];
}
```

### TaskItem
```csharp
public class TaskItem
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string? AssigneeId { get; set; }  // nullable string FK → AppUser.Id
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskPriority Priority { get; set; }
    public TaskStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Project Project { get; set; } = null!;
    public AppUser? Assignee { get; set; }
    public ICollection<TaskAttachment> Attachments { get; set; } = [];  // ← navigation
}

public enum TaskPriority { Low = 0, Medium = 1, High = 2, Critical = 3 }
public enum TaskStatus   { Todo = 0, InProgress = 1, Done = 2, Cancelled = 3 }
```

### TaskAttachment *(new)*
```csharp
public class TaskAttachment
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string UploadedById { get; set; } = string.Empty;  // FK → AppUser.Id

    // User-visible metadata — stored in DB, displayed in responses
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    // Server-controlled storage — NEVER derived from user input
    public string StoredFileName { get; set; } = string.Empty;  // e.g. "3fa85f64.pdf"
    public string StoragePath { get; set; } = string.Empty;     // full absolute path on disk

    // Processing lifecycle
    public AttachmentStatus Status { get; set; }
    public string? ProcessingError { get; set; }  // populated only on Failed

    public DateTime UploadedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public TaskItem Task { get; set; } = null!;
    public AppUser UploadedBy { get; set; } = null!;
}

public enum AttachmentStatus
{
    Pending    = 0,   // bytes in Redis (TTL: 30 min), message published to RabbitMQ
    Processing = 1,   // file-worker received message, reading from Redis
    Ready      = 2,   // worker wrote to permanent disk; StoragePath is set
    Failed     = 3    // worker error — ProcessingError has details; Redis key gone
}
```

---

## AppDbContext

```csharp
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<Workspace>       Workspaces       => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<Project>         Projects          => Set<Project>();
    public DbSet<TaskItem>        Tasks             => Set<TaskItem>();
    public DbSet<TaskAttachment>  TaskAttachments   => Set<TaskAttachment>();  // ← new

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);  // Identity MUST be first
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

---

## Fluent API Configurations

### TaskItemConfiguration
```csharp
public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Title).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Priority).HasConversion<string>();
        builder.Property(t => t.Status).HasConversion<string>();
        builder.Property(t => t.UpdatedAt).IsConcurrencyToken();

        builder.HasIndex(t => t.ProjectId)
               .HasDatabaseName("IX_Tasks_ProjectId");
        builder.HasIndex(t => new { t.AssigneeId, t.Status })
               .HasDatabaseName("IX_Tasks_AssigneeId_Status");
        builder.HasIndex(t => t.DueDate)
               .HasDatabaseName("IX_Tasks_DueDate");

        builder.HasOne(t => t.Project)
               .WithMany(p => p.Tasks)
               .HasForeignKey(t => t.ProjectId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Assignee)
               .WithMany()
               .HasForeignKey(t => t.AssigneeId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
```

### TaskAttachmentConfiguration *(new)*
```csharp
public class TaskAttachmentConfiguration : IEntityTypeConfiguration<TaskAttachment>
{
    public void Configure(EntityTypeBuilder<TaskAttachment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.OriginalFileName).IsRequired().HasMaxLength(260);
        builder.Property(a => a.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(a => a.StoredFileName).IsRequired().HasMaxLength(100);
        builder.Property(a => a.StoragePath).IsRequired().HasMaxLength(500);
        builder.Property(a => a.Status).HasConversion<string>();

        // Cascade delete: task deleted → all its attachments deleted
        builder.HasOne(a => a.Task)
               .WithMany(t => t.Attachments)
               .HasForeignKey(a => a.TaskId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.UploadedBy)
               .WithMany()
               .HasForeignKey(a => a.UploadedById)
               .OnDelete(DeleteBehavior.Restrict);  // don't delete files if user is deleted

        // Indexes
        builder.HasIndex(a => a.TaskId)
               .HasDatabaseName("IX_TaskAttachments_TaskId");

        builder.HasIndex(a => a.Status)
               .HasDatabaseName("IX_TaskAttachments_Status");
        // Status index used by FileProcessingService on startup to recover Pending items
    }
}
```

---

## Index Strategy

| Index | Columns | Reason |
|-------|---------|--------|
| PK composite | `WorkspaceMember(WorkspaceId, UserId)` | Role lookup per request — O(1) |
| `IX_Tasks_ProjectId` | `TaskItem(ProjectId)` | Every task list filters by project |
| `IX_Tasks_AssigneeId_Status` | `TaskItem(AssigneeId, Status)` | "My open tasks" across workspaces |
| `IX_Tasks_DueDate` | `TaskItem(DueDate)` | Deadline scanner |
| `IX_Projects_WorkspaceId` | `Project(WorkspaceId)` | Project list per workspace |
| `IX_TaskAttachments_TaskId` | `TaskAttachment(TaskId)` | Fetch all attachments for a task |
| `IX_TaskAttachments_Status` | `TaskAttachment(Status)` | Recovery scan on service startup |

---

## CTE — Project Task Summary

Single DB round-trip for the project detail summary:

```sql
WITH task_counts AS (
    SELECT status, COUNT(*) AS cnt
    FROM task_items
    WHERE project_id = @projectId
    GROUP BY status
)
SELECT
    COALESCE(SUM(cnt), 0)                                              AS total,
    COALESCE(SUM(CASE WHEN status = 'Todo'       THEN cnt END), 0)    AS todo,
    COALESCE(SUM(CASE WHEN status = 'InProgress' THEN cnt END), 0)    AS in_progress,
    COALESCE(SUM(CASE WHEN status = 'Done'       THEN cnt END), 0)    AS done,
    COALESCE(SUM(CASE WHEN status = 'Cancelled'  THEN cnt END), 0)    AS cancelled
FROM task_counts;
```

---

## Migrations

```bash
# Generate
dotnet ef migrations add <Name> \
  --project TaskFlow.Infrastructure \
  --startup-project TaskFlow.Api

# Apply (or: make migrate)
dotnet ef database update \
  --project TaskFlow.Infrastructure \
  --startup-project TaskFlow.Api
```

Rules: never hand-edit migrations, never auto-apply on startup, always commit the snapshot.

---

## Query Anti-Patterns to Avoid

```csharp
// ❌ ToList then filter — loads entire table into memory
var tasks = await db.Tasks.ToListAsync();
var filtered = tasks.Where(t => t.ProjectId == id).ToList();

// ✅ Filter in DB — uses IX_Tasks_ProjectId
var tasks = await db.Tasks
    .Where(t => t.ProjectId == id)
    .ToListAsync(ct);

// ❌ Lazy load per attachment — N+1
foreach (var task in tasks)
    _ = task.Attachments.Count;

// ✅ Include upfront when attachments are needed (single task detail only)
var task = await db.Tasks
    .Include(t => t.Attachments)
    .Include(t => t.Assignee)
    .FirstOrDefaultAsync(t => t.Id == id, ct);

// Note: the paginated task list does NOT include attachments — too expensive
// Attachments are only loaded on single-task GET /tasks/{id}
```
