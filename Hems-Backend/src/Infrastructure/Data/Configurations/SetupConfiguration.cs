using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hems_Backend.Infrastructure.Data.Configurations;

public class SetupConfiguration : IEntityTypeConfiguration<Setup>
{
    public void Configure(EntityTypeBuilder<Setup> builder)
    {
        builder.Property(t => t.Name)
            .HasMaxLength(200)
            .IsRequired()
            .HasDefaultValue("Default Setup");

        // 1:1 relationship with Site - Bir Setup'ın sadece bir Site'ı olabilir
        builder.HasOne(s => s.Site)
            .WithOne(st => st.Setup)
            .HasForeignKey<Setup>(s => s.SiteId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: Bir Site'ta sadece bir Setup olabilir
        builder.HasIndex(s => s.SiteId)
            .IsUnique()
            .HasDatabaseName("IX_Setups_SiteId_Unique");
    }
}








