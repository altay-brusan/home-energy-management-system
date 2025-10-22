using Hems_Backend.Domain.Common;

namespace Hems_Backend.Domain.Entities;

public class ParameterValueSettingScheme : BaseGuidAuditableEntity
{
    public Guid ParameterId { get; set; }
    public Guid? ParameterGroupId { get; set; }
    public string Value { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string ModelNumber { get; set; } = string.Empty;

    // Navigation properties
    public Parameter Parameter { get; set; } = null!;
    public ParameterGroup? ParameterGroup { get; set; }
}
