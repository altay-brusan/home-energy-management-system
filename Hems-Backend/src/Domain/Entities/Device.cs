using Hems_Backend.Domain.Common;
using Hems_Backend.Domain.Enums;
using System.Text.Json;

namespace Hems_Backend.Domain.Entities;

public class Device : BaseGuidAuditableEntity
{
    public Guid ThingsBoardDeviceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string ModelNumber { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? TypeNumber { get; set; }
    public string? QRCode { get; set; }
    public JsonDocument Specifications { get; set; } = JsonDocument.Parse("{}"); // EF Core JSON column
    public bool IsDeleted { get; set; } = false;
    public DateTime? InstallationDate { get; set; }
    public DateTime? CalibrationDate { get; set; }
    public DateTime? NextCheckoutDate { get; set; }
    public int? WarrantyPeriod { get; set; }
    public WarrantyUnit? WarrantyUnit { get; set; }
    public DateTime? WarrantyStartDate { get; set; }

    // Navigation properties
    public ICollection<DeviceComponent> DeviceComponents { get; set; } = new List<DeviceComponent>();
    public SetupDevice? SetupDevice { get; set; } // Bir Device sadece bir Setup'a ait olabilir
}





