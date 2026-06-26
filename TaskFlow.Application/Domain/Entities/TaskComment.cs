namespace TaskFlow.Application.Domain.Entities;

public class TaskComment
{
    public Guid     Id          { get; set; } = Guid.NewGuid();
    public Guid     TaskId      { get; set; }
    public string   Content     { get; set; } = string.Empty;
    public string   CreatedById { get; set; } = string.Empty;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt   { get; set; } = DateTime.UtcNow;

    public WorkspaceTask                   Task     { get; set; } = null!;
    public AppUser                         CreatedBy { get; set; } = null!;
    public ICollection<TaskCommentMention> Mentions  { get; set; } = [];
}
