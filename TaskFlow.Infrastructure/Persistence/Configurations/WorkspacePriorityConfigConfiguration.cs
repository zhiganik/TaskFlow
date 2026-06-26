using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Application.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public class WorkspacePriorityConfigConfiguration : IEntityTypeConfiguration<WorkspacePriorityConfig>
{
    public void Configure(EntityTypeBuilder<WorkspacePriorityConfig> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Priority).HasConversion<string>().IsRequired();
        builder.Property(c => c.DisplayName).IsRequired().HasMaxLength(50);
        builder.Property(c => c.Color).IsRequired().HasMaxLength(7);

        builder.HasIndex(c => new { c.WorkspaceId, c.Priority })
            .IsUnique()
            .HasDatabaseName("IX_WorkspacePriorityConfigs_WorkspaceId_Priority");

        builder.HasOne(c => c.Workspace)
            .WithMany(w => w.PriorityConfigs)
            .HasForeignKey(c => c.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
