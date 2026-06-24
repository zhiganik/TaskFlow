using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Application.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name).IsRequired().HasMaxLength(100);
        builder.Property(w => w.OwnerId).IsRequired();

        builder.HasIndex(w => w.OwnerId)
               .HasDatabaseName("IX_Workspaces_OwnerId");

        builder.HasOne(w => w.Owner)
               .WithMany()
               .HasForeignKey(w => w.OwnerId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
