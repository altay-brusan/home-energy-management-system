namespace Hems_Backend.Application.ParameterGroups.Queries.GetParameterGroups;

public class ParameterGroupDto
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Version { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
    public List<ParameterGroupDto> Children { get; set; } = new();
    public List<ParameterDto> Parameters { get; set; } = new();
}

public class ParameterDto
{
    public Guid Id { get; set; }
    public Guid ParameterGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ElementType { get; set; } = string.Empty;
    public string RoleType { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string DefaultValue { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}

public class ParameterGroupsVm
{
    public List<ParameterGroupDto> ParameterGroups { get; set; } = new();
}
