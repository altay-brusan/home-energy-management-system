using Hems_Backend.Domain.Common;

namespace Hems_Backend.Domain.Entities;

public class Country : BaseGuidAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // ISO country code (TR, US, etc.)
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<City> Cities { get; set; } = new List<City>();
}
