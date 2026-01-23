using MediatR;

namespace Hems_Backend.Application.DeviceComponents.Queries.GetDeviceDetails;

/// <summary>
/// Query to get detailed information about a device component
/// </summary>
public record GetDeviceDetailsQuery : IRequest<DeviceDetailsResponseDto?>
{
    /// <summary>
    /// The Device Component ID
    /// </summary>
    public Guid ComponentId { get; init; }

    /// <summary>
    /// Optional BMS address for battery components.
    /// When provided, returns data for that specific BMS module.
    /// When null, returns aggregate/overall battery data.
    /// </summary>
    public int? BmsAddress { get; init; }
}
