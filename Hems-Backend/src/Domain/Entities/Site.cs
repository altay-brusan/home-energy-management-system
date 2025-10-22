using Hems_Backend.Domain.Common;

namespace Hems_Backend.Domain.Entities;

public class Site : BaseGuidAuditableEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? AddressLine { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string Timezone { get; set; } = "Europe/Istanbul";
    public bool IsDeleted { get; set; } = false;

    // Navigation property - Bir Site'ın sadece bir Setup'ı olabilir
    public Setup? Setup { get; set; }
}

