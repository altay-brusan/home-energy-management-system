using Hems_Backend.Domain.Common;

namespace Hems_Backend.Domain.Entities;

public class SetupDevice : BaseGuidAuditableEntity
{
    public Guid SetupId { get; set; }
    public Guid DeviceId { get; set; }

    // Navigation properties
    public Setup Setup { get; set; } = null!;
    public Device Device { get; set; } = null!;
    public ICollection<ExternalDevice> ExternalDevices { get; set; } = new List<ExternalDevice>();
}


