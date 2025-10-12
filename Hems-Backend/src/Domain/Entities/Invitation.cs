using Hems_Backend.Domain.Common;
using Hems_Backend.Domain.Enums;

namespace Hems_Backend.Domain.Entities;

public class Invitation : BaseGuidAuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string? InvitedUserId { get; set; }
    public string InvitedByUserId { get; set; } = string.Empty;
    public Guid SiteId { get; set; }
    public string Token { get; set; } = string.Empty;
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    public DateTimeOffset? AcceptanceTime { get; set; }

    // Navigation properties
    public Site Site { get; set; } = null!;
}

