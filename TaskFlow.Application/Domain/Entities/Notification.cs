using TaskFlow.Application.Domain.Enums;

namespace TaskFlow.Application.Domain.Entities;

public class Notification
{
    public Guid             Id          { get; set; } = Guid.NewGuid();
    public string           RecipientId { get; set; } = string.Empty;
    public NotificationType Type        { get; set; }
    public string           Title       { get; set; } = string.Empty;
    public string           Body        { get; set; } = string.Empty;
    public bool             IsRead      { get; set; }
    public DateTime         CreatedAt   { get; set; } = DateTime.UtcNow;

    public Guid?  WorkspaceId { get; set; }
    public Guid?  TaskId      { get; set; }
    public Guid?  CommentId   { get; set; }

    public AppUser Recipient  { get; set; } = null!;
}
