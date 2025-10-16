using Hems_Backend.Application.Common.Models;
using MediatR;

namespace Hems_Backend.Application.Devices.Queries.GetComponentLiveData;

public record GetComponentLiveDataQuery : IRequest<GetComponentLiveDataResponse>
{
    public Guid DeviceId { get; init; }
    public Guid ComponentId { get; init; }
}
