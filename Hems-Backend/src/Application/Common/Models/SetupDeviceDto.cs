namespace Hems_Backend.Application.Common.Models;

public class SetupDeviceDto
{
    public Guid Id { get; set; }
    public Guid SetupId { get; set; }
    public Guid DeviceId { get; set; }
    public DeviceDto Device { get; set; } = null!;
    public List<ExternalDeviceDto> ExternalDevices { get; set; } = new();
    public DateTime Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}


