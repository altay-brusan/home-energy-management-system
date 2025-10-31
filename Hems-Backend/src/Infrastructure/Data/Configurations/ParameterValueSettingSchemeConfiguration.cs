using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hems_Backend.Infrastructure.Data.Configurations;

public class ParameterValueSettingSchemeConfiguration : IEntityTypeConfiguration<ParameterValueSettingScheme>
{
    public void Configure(EntityTypeBuilder<ParameterValueSettingScheme> builder)
    {
        builder.Property(t => t.Value)
            .IsRequired();

        builder.Property(t => t.ModelNumber)
            .HasMaxLength(100)
            .IsRequired();

        // Foreign key relationships
        builder.HasOne(pvss => pvss.Parameter)
            .WithMany(p => p.ParameterValueSettingSchemes)
            .HasForeignKey(pvss => pvss.ParameterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pvss => pvss.ParameterGroup)
            .WithMany()
            .HasForeignKey(pvss => pvss.ParameterGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for performance
        builder.HasIndex(x => x.ParameterId)
            .HasDatabaseName("IX_ParameterValueSettingSchemes_ParameterId");

        builder.HasIndex(x => x.ParameterGroupId)
            .HasDatabaseName("IX_ParameterValueSettingSchemes_ParameterGroupId");

        builder.HasIndex(x => x.ModelNumber)
            .HasDatabaseName("IX_ParameterValueSettingSchemes_ModelNumber");

        builder.HasIndex(x => x.SortOrder)
            .HasDatabaseName("IX_ParameterValueSettingSchemes_SortOrder");

        builder.HasIndex(x => new { x.ModelNumber, x.SortOrder })
            .HasDatabaseName("IX_ParameterValueSettingSchemes_ModelNumber_SortOrder");
    }
}
