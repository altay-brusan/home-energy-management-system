using Hems_Backend.Domain.Common;
using Hems_Backend.Domain.Enums;

namespace Hems_Backend.Domain.Entities;

public class UserSite : BaseGuidAuditableEntity
{
    public string UserId { get; set; } = string.Empty;
    public Guid SiteId { get; set; }
    public UserSiteRole Role { get; set; }

    // Navigation properties
    public Site Site { get; set; } = null!;
}

