using Hems_Backend.Domain.Common;

namespace Hems_Backend.Domain.Entities;

public class ParameterGroup : BaseGuidAuditableEntity
{
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Version { get; set; } = string.Empty;
    public int SortOrder { get; set; } 

    // Navigation properties
    public ParameterGroup? Parent { get; set; }
    public ICollection<ParameterGroup> Children { get; set; } = new List<ParameterGroup>();
    public ICollection<Parameter> Parameters { get; set; } = new List<Parameter>();
}
