using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Application.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public class WorkspaceLabelConfiguration : IEntityTypeConfiguration<WorkspaceLabel>
{
    public void Configure(EntityTypeBuilder<WorkspaceLabel> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name).IsRequired().HasMaxLength(50);
        builder.Property(l => l.Color).IsRequired().HasMaxLength(7);

        builder.HasIndex(l => l.WorkspaceId)
            .HasDatabaseName("IX_WorkspaceLabels_WorkspaceId");

        builder.HasOne(l => l.Workspace)
            .WithMany(w => w.Labels)
            .HasForeignKey(l => l.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(l => l.Tasks)
            .WithMany(t => t.Labels)
            .UsingEntity<TaskLabel>(
                j => j.HasOne(tl => tl.Task).WithMany().HasForeignKey(tl => tl.TaskId).OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne(tl => tl.Label).WithMany().HasForeignKey(tl => tl.LabelId).OnDelete(DeleteBehavior.Cascade),
                j => j.HasKey(tl => new { tl.TaskId, tl.LabelId }));
    }
}
