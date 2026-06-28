using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Application.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public class WorkspaceColumnConfiguration : IEntityTypeConfiguration<WorkspaceColumn>
{
    public void Configure(EntityTypeBuilder<WorkspaceColumn> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Color)
            .IsRequired()
            .HasMaxLength(7);

        builder.Property(c => c.WorkspaceId)
            .IsRequired();

        builder.Property(c => c.IsDoneColumn)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(c => c.WorkspaceId)
            .HasDatabaseName("IX_WorkspaceColumns_WorkspaceId");

        builder.HasOne(c => c.Workspace)
            .WithMany(w => w.Columns)
            .HasForeignKey(c => c.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
