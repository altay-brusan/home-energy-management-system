namespace Hems_Backend.Application.Common.Models;

public record DeviceWithDataResponseDto
{
    public Guid SiteId { get; init; }
    public Guid SetupId { get; init; }
    public Guid DeviceId { get; init; }
    public string ModelNumber { get; init; } = string.Empty;
    public string DeviceManufacturer { get; init; } = string.Empty;
    public List<ComponentDto> Components { get; init; } = new();
}

public record ComponentDto
{
    public Guid ComponentId { get; init; }
    public string ComponentName { get; init; } = string.Empty;
    public string ComponentDeviceType { get; init; } = string.Empty;
    public string SerialNumber { get; init; } = string.Empty;
    public object? ComponentData { get; init; }
}
