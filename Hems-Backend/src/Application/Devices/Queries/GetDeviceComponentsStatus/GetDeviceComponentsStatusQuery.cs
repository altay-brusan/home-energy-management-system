using Hems_Backend.Application.Common.Models;
using MediatR;

namespace Hems_Backend.Application.Devices.Queries.GetDeviceComponentsStatus;

public record GetDeviceComponentsStatusQuery : IRequest<DeviceWithDataResponseDto>
{
    public Guid DeviceId { get; init; }
}
