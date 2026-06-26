using TaskFlow.Application.Domain.Enums;

namespace TaskFlow.Application.Domain.Entities;

public class WorkspacePriorityConfig
{
    public Guid         Id          { get; set; } = Guid.NewGuid();
    public Guid         WorkspaceId { get; set; }
    public TaskPriority Priority    { get; set; }
    public string       DisplayName { get; set; } = string.Empty;
    public string       Color       { get; set; } = string.Empty;

    public Workspace Workspace { get; set; } = null!;
}
