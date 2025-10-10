using MediatR;

namespace Hems_Backend.Application.Dashboard.Queries.GetBatteryComponent;

public record GetBatteryComponentQuery : IRequest<BatteryComponentDto>
{
    public Guid DeviceId { get; init; }
}
