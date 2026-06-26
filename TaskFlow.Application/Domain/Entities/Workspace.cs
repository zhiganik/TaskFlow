namespace TaskFlow.Application.Domain.Entities;

public class Workspace
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser Owner { get; set; } = null!;
    public ICollection<WorkspaceMember>        Members         { get; set; } = [];
    public ICollection<WorkspaceColumn>        Columns         { get; set; } = [];
    public ICollection<WorkspaceTask>          Tasks           { get; set; } = [];
    public ICollection<WorkspaceLabel>         Labels          { get; set; } = [];
    public ICollection<WorkspacePriorityConfig> PriorityConfigs { get; set; } = [];
}