namespace Hems_Backend.Application.SetupDeviceParameterValues.Queries.GetSetupDeviceParameterValuesTree;

public class ParameterGroupTreeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Version { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public Guid? ParentId { get; set; }
    public List<ParameterGroupTreeDto> Children { get; set; } = new();
    public List<ParameterTreeDto> Parameters { get; set; } = new();
}

public class ParameterTreeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ElementType { get; set; } = string.Empty;
    public string RoleType { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string DefaultValue { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public SetupDeviceParameterValueTreeDto SetupDeviceParameterValue { get; set; } = new();
}

public class SetupDeviceParameterValueTreeDto
{
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string ModelNumber { get; set; } = string.Empty;
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}

public class SetupDeviceParameterValuesTreeVm
{
    public Guid SetupDeviceId { get; set; }
    public List<ParameterGroupTreeDto> ParameterGroups { get; set; } = new();
}
