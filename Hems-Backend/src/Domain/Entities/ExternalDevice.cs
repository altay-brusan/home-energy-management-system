using Hems_Backend.Domain.Common;
using System.Text.Json;

namespace Hems_Backend.Domain.Entities;

public class ExternalDevice : BaseGuidAuditableEntity
{
    public Guid SetupDeviceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty; // SolarPanel, Charger, WindTurbine, etc.
    public JsonDocument Specifications { get; set; } = JsonDocument.Parse("{}"); // EF Core JSON column

    // Navigation property
    public SetupDevice SetupDevice { get; set; } = null!;
}
