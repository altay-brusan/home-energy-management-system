using System.Text.Json;
using Hems_Backend.Domain.Common;

namespace Hems_Backend.Domain.Entities;

public class DeviceData : BaseAuditableEntity
{
    public string DataType { get; set; } = string.Empty;
    
    public Guid ThingsBoardDeviceId { get; set; }
    
    public DateTime RequestDate { get; set; }
    
    public JsonDocument Data { get; set; } = JsonDocument.Parse("{}");
    
    // Navigation property
    public Device Device { get; set; } = null!;
}
