using System.Text.Json;

namespace Hems_Backend.Application.Common.Models;

public class ExternalDeviceDto
{
    public Guid Id { get; set; }
    public Guid SetupDeviceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public JsonDocument Specifications { get; set; } = JsonDocument.Parse("{}");
    public DateTime Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}
