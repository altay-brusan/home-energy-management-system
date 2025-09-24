using Hems_Backend.Domain.Enums;
using System.Text.Json;

namespace Hems_Backend.Application.Common.Models;

public class DeviceComponentDto
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public DeviceType DeviceType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? ModelNumber { get; set; }
    public string? SerialNumber { get; set; }
    public JsonDocument Specifications { get; set; } = JsonDocument.Parse("{}");
    public int? WarrantyPeriod { get; set; }
    public WarrantyUnit? WarrantyUnit { get; set; }
    public DateTime? WarrantyStartDate { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}
