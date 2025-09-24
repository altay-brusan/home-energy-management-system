using Hems_Backend.Domain.Common;

namespace Hems_Backend.Domain.Entities;

public class Setup : BaseGuidAuditableEntity
{
    public Guid SiteId { get; set; }
    public string Name { get; set; } = "Default Setup";
    
    // Navigation properties
    public Site Site { get; set; } = null!; // Bir Setup'ın sadece bir Site'ı olabilir
    public SetupDevice? SetupDevice { get; set; } // Bir Setup'ın sadece bir SetupDevice'i olabilir
}






