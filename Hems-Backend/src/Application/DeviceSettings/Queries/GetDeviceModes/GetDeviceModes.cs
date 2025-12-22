using Hems_Backend.Application.DeviceSettings.Models;
using MediatR;

namespace Hems_Backend.Application.DeviceSettings.Queries.GetDeviceModes;

public record GetDeviceModesQuery : IRequest<DeviceModesVm>
{
}

public class GetDeviceModesQueryHandler : IRequestHandler<GetDeviceModesQuery, DeviceModesVm>
{
    public Task<DeviceModesVm> Handle(GetDeviceModesQuery request, CancellationToken cancellationToken)
    {
        var modes = DeviceModeConfigurations.GetAllModesWithDescriptions()
            .Select(kvp => new DeviceModeDto
            {
                Mode = kvp.Key,
                Name = kvp.Key.ToString(),
                Description = kvp.Value
            })
            .ToList();

        return Task.FromResult(new DeviceModesVm
        {
            Modes = modes
        });
    }
}

/// <summary>
/// View model for device modes
/// </summary>
public record DeviceModesVm
{
    public List<DeviceModeDto> Modes { get; init; } = new();
}

/// <summary>
/// DTO for a device mode
/// </summary>
public record DeviceModeDto
{
    public DeviceMode Mode { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Dictionary<string, string>? Configuration { get; init; }
}
