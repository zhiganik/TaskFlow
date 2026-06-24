using TaskFlow.Application.Domain.Enums;

namespace TaskFlow.Application.Domain.Entities;

public class WorkspaceMember
{
    public Guid WorkspaceId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public WorkspaceRole Role { get; set; }
    public DateTime JoinedAt { get; set; }

    public Workspace Workspace { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}