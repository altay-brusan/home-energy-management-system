using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hems_Backend.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(t => t.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(t => t.UserId)
            .HasMaxLength(450); // For Identity User ID length

        builder.Property(t => t.Message)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes for performance
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Email);
        builder.HasIndex(x => x.InvitationId);
        builder.HasIndex(x => new { x.UserId, x.IsRead });

        // Relationship with Invitation
        builder.HasOne(n => n.Invitation)
            .WithMany()
            .HasForeignKey(n => n.InvitationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

