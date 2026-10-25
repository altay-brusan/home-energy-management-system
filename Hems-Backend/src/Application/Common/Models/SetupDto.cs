namespace Hems_Backend.Application.Common.Models;

public class SetupDto
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public SetupDeviceDto? SetupDevice { get; set; }
    public DateTime Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}






