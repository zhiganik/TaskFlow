using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Application.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public class WorkspaceMemberConfiguration : IEntityTypeConfiguration<WorkspaceMember>
{
    public void Configure(EntityTypeBuilder<WorkspaceMember> builder)
    {
        builder.HasKey(m => new { m.WorkspaceId, m.UserId });

        builder.Property(m => m.Role).HasConversion<string>();

        builder.HasOne(m => m.Workspace)
               .WithMany(w => w.Members)
               .HasForeignKey(m => m.WorkspaceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.User)
               .WithMany()
               .HasForeignKey(m => m.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
