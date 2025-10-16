using Hems_Backend.Application.Common.Models;
using MediatR;

namespace Hems_Backend.Application.Devices.Queries.GetDeviceWithData;

public record GetDeviceWithDataQuery : IRequest<DeviceWithDataResponseDto>
{
    public Guid DeviceId { get; init; }
}
