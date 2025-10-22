namespace Hems_Backend.Application.ParameterValueSettingSchemes.Queries.GetParameterValueSettingSchemes;

public class ParameterValueSettingSchemeDto
{
    public Guid Id { get; set; }
    public Guid ParameterId { get; set; }
    public Guid? ParameterGroupId { get; set; }
    public string Value { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string ModelNumber { get; set; } = string.Empty;
    public string? ParameterName { get; set; }
    public string? ParameterGroupName { get; set; }
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}

public class ParameterValueSettingSchemesVm
{
    public List<ParameterValueSettingSchemeDto> ParameterValueSettingSchemes { get; set; } = new();
}
