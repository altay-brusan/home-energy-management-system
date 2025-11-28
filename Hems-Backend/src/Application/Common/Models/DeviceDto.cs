using Hems_Backend.Domain.Enums;
using System.Text.Json;

namespace Hems_Backend.Application.Common.Models;

public class DeviceDto
{
    public Guid Id { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string? ModelNumber { get; set; }
    public string? SerialNumber { get; set; }
    public string? TypeNumber { get; set; }
    public string? QRCode { get; set; }
    public JsonDocument Specifications { get; set; } = JsonDocument.Parse("{}");
    public bool IsDeleted { get; set; }
    public DateTime? InstallationDate { get; set; }
    public DateTime? CalibrationDate { get; set; }
    public DateTime? NextCheckoutDate { get; set; }
    public int? WarrantyPeriod { get; set; }
    public WarrantyUnit? WarrantyUnit { get; set; }
    public DateTime? WarrantyStartDate { get; set; }
    public List<DeviceComponentDto> DeviceComponents { get; set; } = new();
    public SetupDeviceDto? SetupDevice { get; set; }
    public DateTime Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}





