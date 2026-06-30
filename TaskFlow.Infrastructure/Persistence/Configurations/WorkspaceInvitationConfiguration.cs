using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Application.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public class WorkspaceInvitationConfiguration : IEntityTypeConfiguration<WorkspaceInvitation>
{
    public void Configure(EntityTypeBuilder<WorkspaceInvitation> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Email).IsRequired();
        builder.Property(i => i.Token).IsRequired();
        builder.Property(i => i.Role).HasConversion<string>();

        builder.HasIndex(i => i.Token).IsUnique();
        builder.HasIndex(i => new { i.WorkspaceId, i.Email }).IsUnique();

        builder.HasOne(i => i.Workspace)
               .WithMany()
               .HasForeignKey(i => i.WorkspaceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.InvitedBy)
               .WithMany()
               .HasForeignKey(i => i.InvitedById)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
