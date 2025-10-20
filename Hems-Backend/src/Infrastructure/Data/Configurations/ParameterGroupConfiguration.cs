using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hems_Backend.Infrastructure.Data.Configurations;

public class ParameterGroupConfiguration : IEntityTypeConfiguration<ParameterGroup>
{
    public void Configure(EntityTypeBuilder<ParameterGroup> builder)
    {
        builder.Property(t => t.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.Version)
            .HasMaxLength(50)
            .IsRequired();

        // Self-referencing relationship for hierarchical structure
        builder.HasOne(pg => pg.Parent)
            .WithMany(pg => pg.Children)
            .HasForeignKey(pg => pg.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for performance
        builder.HasIndex(x => x.ParentId)
            .HasDatabaseName("IX_ParameterGroups_ParentId");


        builder.HasIndex(x => x.SortOrder)
            .HasDatabaseName("IX_ParameterGroups_SortOrder");

        builder.HasIndex(x => new { x.ParentId, x.SortOrder })
            .HasDatabaseName("IX_ParameterGroups_ParentId_SortOrder");
    }
}
