namespace TaskFlow.Application.Domain.Entities;

public class WorkspaceLabel
{
    public Guid     Id          { get; set; } = Guid.NewGuid();
    public Guid     WorkspaceId { get; set; }
    public string   Name        { get; set; } = string.Empty;
    public string   Color       { get; set; } = string.Empty;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;

    public Workspace                  Workspace { get; set; } = null!;
    public ICollection<WorkspaceTask> Tasks     { get; set; } = [];
}
