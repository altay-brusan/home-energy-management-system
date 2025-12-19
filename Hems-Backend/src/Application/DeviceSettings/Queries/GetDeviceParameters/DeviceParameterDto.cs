namespace Hems_Backend.Application.DeviceSettings.Queries.GetDeviceParameters;

public class DeviceParameterDto
{
    public Guid Id { get; set; }
    public Guid ParameterId { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public string ParameterDescription { get; set; } = string.Empty;
    public Guid? ParameterGroupId { get; set; }
    public string? ParameterGroupName { get; set; }
    public string Value { get; set; } = string.Empty;
    public string ElementType { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string DefaultValue { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string ModelNumber { get; set; } = string.Empty;
    public DateTimeOffset? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}
