namespace Hems_Backend.Application.ParameterValueSettingSchemes.Queries.GetParameterValueSettingSchemesTree;

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
    public ParameterValueSettingSchemeTreeDto ParameterValueSettingScheme { get; set; } = new();
}

public class ParameterValueSettingSchemeTreeDto
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

public class ParameterValueSettingSchemesTreeVm
{
    public string? ModelNumber { get; set; }
    public List<ParameterGroupTreeDto> ParameterGroups { get; set; } = new();
}
