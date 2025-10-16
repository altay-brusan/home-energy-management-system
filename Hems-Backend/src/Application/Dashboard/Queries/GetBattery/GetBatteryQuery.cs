using Hems_Backend.Application.Common.Models;
using MediatR;

namespace Hems_Backend.Application.Dashboard.Queries.GetBattery;

public record GetBatteryQuery : IRequest<BatteryResponseDto>
{
    public Guid DeviceId { get; init; }
}
