namespace TaskFlow.Application.Domain.Entities;

public class TaskLabel
{
    public Guid TaskId  { get; set; }
    public Guid LabelId { get; set; }

    public WorkspaceTask  Task  { get; set; } = null!;
    public WorkspaceLabel Label { get; set; } = null!;
}
