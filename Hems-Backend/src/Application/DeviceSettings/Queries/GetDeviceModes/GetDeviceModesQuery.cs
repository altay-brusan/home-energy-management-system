using Hems_Backend.Application.DeviceSettings.Models;
using MediatR;

namespace Hems_Backend.Application.DeviceSettings.Queries.GetDeviceModes;

public record GetDeviceModesQuery : IRequest<List<DeviceModeInfo>>
{
}

public class GetDeviceModesQueryHandler : IRequestHandler<GetDeviceModesQuery, List<DeviceModeInfo>>
{
    public Task<List<DeviceModeInfo>> Handle(GetDeviceModesQuery request, CancellationToken cancellationToken)
    {
        var modes = DeviceModeConfigurations.GetAvailableModes();
        return Task.FromResult(modes);
    }
}
