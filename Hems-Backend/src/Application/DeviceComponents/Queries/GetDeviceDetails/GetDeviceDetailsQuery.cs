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
}
