using Hems_Backend.Domain.Common;

namespace Hems_Backend.Domain.Entities;

public class City : BaseGuidAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid CountryId { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Country Country { get; set; } = null!;
}
