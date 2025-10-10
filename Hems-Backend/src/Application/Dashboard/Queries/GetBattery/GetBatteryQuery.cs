using MediatR;

namespace Hems_Backend.Application.Dashboard.Queries.GetBattery;

public record GetBatteryQuery : IRequest<BatteryDto>
{
    public Guid DeviceId { get; init; }
}
