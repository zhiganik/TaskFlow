namespace TaskFlow.Application.Domain.Entities;

public class TaskCommentMention
{
    public Guid   CommentId { get; set; }
    public string UserId    { get; set; } = string.Empty;

    public TaskComment Comment { get; set; } = null!;
    public AppUser     User    { get; set; } = null!;
}
