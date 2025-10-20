using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hems_Backend.Infrastructure.Data.Configurations;

public class SetupDeviceParameterValueConfiguration : IEntityTypeConfiguration<SetupDeviceParameterValue>
{
    public void Configure(EntityTypeBuilder<SetupDeviceParameterValue> builder)
    {
        builder.Property(t => t.Value)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(t => t.ModelNumber)
            .HasMaxLength(100)
            .IsRequired();

        // Foreign key relationships
        builder.HasOne(sdpv => sdpv.SetupDevice)
            .WithMany()
            .HasForeignKey(sdpv => sdpv.SetupDeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sdpv => sdpv.Parameter)
            .WithMany(p => p.SetupDeviceParameterValues)
            .HasForeignKey(sdpv => sdpv.ParameterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sdpv => sdpv.ParameterGroup)
            .WithMany()
            .HasForeignKey(sdpv => sdpv.ParameterGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint for (SetupDeviceId, ParameterId)
        builder.HasIndex(x => new { x.SetupDeviceId, x.ParameterId })
            .IsUnique()
            .HasDatabaseName("IX_SetupDeviceParameterValues_SetupDeviceId_ParameterId");

        // Indexes for performance
        builder.HasIndex(x => x.SetupDeviceId)
            .HasDatabaseName("IX_SetupDeviceParameterValues_SetupDeviceId");

        builder.HasIndex(x => x.ParameterId)
            .HasDatabaseName("IX_SetupDeviceParameterValues_ParameterId");

        builder.HasIndex(x => x.ParameterGroupId)
            .HasDatabaseName("IX_SetupDeviceParameterValues_ParameterGroupId");
    }
}
