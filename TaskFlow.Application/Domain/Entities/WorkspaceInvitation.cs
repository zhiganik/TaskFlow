using TaskFlow.Application.Domain.Enums;

namespace TaskFlow.Application.Domain.Entities;

public class WorkspaceInvitation
{
    public Guid            Id          { get; set; }
    public Guid            WorkspaceId { get; set; }
    public string          Email       { get; set; } = string.Empty;
    public WorkspaceRole   Role        { get; set; }
    public string          Token       { get; set; } = string.Empty;
    public string          InvitedById { get; set; } = string.Empty;
    public DateTime        CreatedAt   { get; set; }
    public DateTime        ExpiresAt   { get; set; }

    public Workspace Workspace { get; set; } = null!;
    public AppUser   InvitedBy { get; set; } = null!;
}
