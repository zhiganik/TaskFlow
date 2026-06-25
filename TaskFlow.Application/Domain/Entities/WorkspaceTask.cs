using TaskFlow.Application.Domain.Enums;

namespace TaskFlow.Application.Domain.Entities;

public class WorkspaceTask
{
    public Guid         Id          { get; set; } = Guid.NewGuid();
    public Guid         WorkspaceId { get; set; }
    public Guid         ColumnId    { get; set; }
    public string       Title       { get; set; } = string.Empty;
    public string?      Description { get; set; }
    public int          Order       { get; set; }
    public TaskPriority Priority    { get; set; } = TaskPriority.Medium;
    public string?      AssigneeId  { get; set; }
    public DateTime?    DueDate     { get; set; }
    public string       CreatedById { get; set; } = string.Empty;
    public DateTime     CreatedAt   { get; set; } = DateTime.UtcNow;

    public Workspace       Workspace { get; set; } = null!;
    public WorkspaceColumn Column    { get; set; } = null!;
    public AppUser?        Assignee  { get; set; }
    public AppUser         CreatedBy { get; set; } = null!;
}
