using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hems_Backend.Infrastructure.Data.Configurations;

public class ParameterConfiguration : IEntityTypeConfiguration<Parameter>
{
    public void Configure(EntityTypeBuilder<Parameter> builder)
    {
        builder.Property(t => t.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.ElementType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.RoleType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.Unit)
            .HasMaxLength(50);

        builder.Property(t => t.DefaultValue)
            .HasMaxLength(1000);

        // Foreign key relationship with ParameterGroup
        builder.HasOne(p => p.ParameterGroup)
            .WithMany(pg => pg.Parameters)
            .HasForeignKey(p => p.ParameterGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for performance
        builder.HasIndex(x => x.ParameterGroupId)
            .HasDatabaseName("IX_Parameters_ParameterGroupId");


        builder.HasIndex(x => x.SortOrder)
            .HasDatabaseName("IX_Parameters_SortOrder");


        builder.HasIndex(x => new { x.ParameterGroupId, x.SortOrder })
            .HasDatabaseName("IX_Parameters_ParameterGroupId_SortOrder");
    }
}
