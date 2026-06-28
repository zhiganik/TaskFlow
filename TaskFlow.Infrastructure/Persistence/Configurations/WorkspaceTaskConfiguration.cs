using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public class WorkspaceTaskConfiguration : IEntityTypeConfiguration<WorkspaceTask>
{
    public void Configure(EntityTypeBuilder<WorkspaceTask> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Number).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        builder.HasIndex(t => new { t.WorkspaceId, t.Number })
            .IsUnique()
            .HasDatabaseName("IX_WorkspaceTasks_WorkspaceId_Number");

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(2000);

        builder.Property(t => t.Priority)
            .HasConversion<string>();

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasDefaultValue(WorkspaceTaskStatus.Active);

        builder.HasIndex(t => new { t.WorkspaceId, t.Status })
            .HasDatabaseName("IX_WorkspaceTasks_WorkspaceId_Status");

        builder.HasIndex(t => new { t.Status, t.CompletedAt })
            .HasDatabaseName("IX_WorkspaceTasks_Status_CompletedAt");

        builder.HasIndex(t => new { t.WorkspaceId, t.ClosedAt })
            .HasDatabaseName("IX_WorkspaceTasks_WorkspaceId_ClosedAt");

        builder.Property(t => t.WorkspaceId).IsRequired();
        builder.Property(t => t.ColumnId).IsRequired();
        builder.Property(t => t.CreatedById).IsRequired();

        builder.HasIndex(t => t.WorkspaceId)
            .HasDatabaseName("IX_WorkspaceTasks_WorkspaceId");

        builder.HasIndex(t => t.ColumnId)
            .HasDatabaseName("IX_WorkspaceTasks_ColumnId");

        builder.HasIndex(t => new { t.ColumnId, t.Order })
            .HasDatabaseName("IX_WorkspaceTasks_ColumnId_Order");

        builder.HasOne(t => t.Workspace)
            .WithMany(w => w.Tasks)
            .HasForeignKey(t => t.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Column)
            .WithMany(c => c.Tasks)
            .HasForeignKey(t => t.ColumnId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Assignee)
            .WithMany()
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.CreatedBy)
            .WithMany()
            .HasForeignKey(t => t.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
