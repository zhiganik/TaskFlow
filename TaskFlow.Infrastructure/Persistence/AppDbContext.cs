using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence;

public sealed class AppDbContext : IdentityDbContext<AppUser>
{
    public DbSet<Workspace>               Workspaces            => Set<Workspace>();
    public DbSet<WorkspaceMember>         WorkspaceMembers      => Set<WorkspaceMember>();
    public DbSet<WorkspaceColumn>         WorkspaceColumns      => Set<WorkspaceColumn>();
    public DbSet<WorkspaceTask>           WorkspaceTasks        => Set<WorkspaceTask>();
    public DbSet<TaskComment>             TaskComments          => Set<TaskComment>();
    public DbSet<TaskCommentMention>      TaskCommentMentions   => Set<TaskCommentMention>();
    public DbSet<WorkspaceLabel>          WorkspaceLabels          => Set<WorkspaceLabel>();
    public DbSet<WorkspacePriorityConfig> WorkspacePriorityConfigs => Set<WorkspacePriorityConfig>();
    public DbSet<TaskLabel>               TaskLabels               => Set<TaskLabel>();
    public DbSet<TaskAttachment>          TaskAttachments          => Set<TaskAttachment>();
    public DbSet<Notification>            Notifications            => Set<Notification>();
    public DbSet<WorkspaceInvitation>     WorkspaceInvitations     => Set<WorkspaceInvitation>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}