using Hems_Backend.Domain.Common;

namespace Hems_Backend.Domain.Entities;

public class Notification : BaseGuidAuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public Guid InvitationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;

    // Navigation property
    public Invitation Invitation { get; set; } = null!;
}

