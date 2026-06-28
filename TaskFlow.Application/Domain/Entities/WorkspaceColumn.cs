namespace TaskFlow.Application.Domain.Entities;

public class WorkspaceColumn
{
    public Guid     Id          { get; set; } = Guid.NewGuid();
    public Guid     WorkspaceId { get; set; }
    public string   Name        { get; set; } = string.Empty;
    public string   Color       { get; set; } = string.Empty;
    public int      Order        { get; set; }
    public bool     IsDoneColumn { get; set; }
    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;

    public Workspace                    Workspace { get; set; } = null!;
    public ICollection<WorkspaceTask>   Tasks     { get; set; } = [];
}
