using Hems_Backend.Application.Common.Models;
using MediatR;

namespace Hems_Backend.Application.Dashboard.Queries.GetEnergySourceMix;

/// <summary>
/// Query to get energy source mix for a specific device and date.
/// Returns pie chart data showing how much energy came from each source (Solar, Battery, Grid, Generator).
/// </summary>
public record GetEnergySourceMixQuery : IRequest<EnergySourceMixDto>
{
    /// <summary>
    /// Local device ID (not ThingsBoard ID)
    /// </summary>
    public Guid DeviceId { get; init; }

    /// <summary>
    /// Date for which to calculate the energy mix (in device local time)
    /// </summary>
    public DateOnly Date { get; init; }
}
