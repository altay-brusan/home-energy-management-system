using Hems_Backend.Domain.Common;

namespace Hems_Backend.Domain.Entities;

public class Parameter : BaseGuidAuditableEntity
{
    public Guid ParameterGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ElementType { get; set; } = string.Empty;
    public string RoleType { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string DefaultValue { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool Visible { get; set; } = true;

    // Navigation properties
    public ParameterGroup ParameterGroup { get; set; } = null!;
    public ICollection<SetupDeviceParameterValue> SetupDeviceParameterValues { get; set; } = new List<SetupDeviceParameterValue>();
    public ICollection<ParameterValueSettingScheme> ParameterValueSettingSchemes { get; set; } = new List<ParameterValueSettingScheme>();
}
