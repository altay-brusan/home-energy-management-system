using Hems_Backend.Domain.Common;
using Hems_Backend.Domain.Enums;
using System.Text.Json;

namespace Hems_Backend.Domain.Entities;

public class DeviceComponent : BaseGuidAuditableEntity
{
    public Guid DeviceId { get; set; }
    public DeviceType DeviceType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? ModelNumber { get; set; }
    public string? SerialNumber { get; set; }
    public JsonDocument Specifications { get; set; } = JsonDocument.Parse("{}"); // EF Core JSON column
    public int? WarrantyPeriod { get; set; }
    public WarrantyUnit? WarrantyUnit { get; set; }
    public DateTime? WarrantyStartDate { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation property
    public Device Device { get; set; } = null!;
}
