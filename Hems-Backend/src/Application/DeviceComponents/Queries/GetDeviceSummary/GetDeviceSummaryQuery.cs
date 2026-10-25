using Hems_Backend.Application.Common.Models;
using MediatR;

namespace Hems_Backend.Application.DeviceComponents.Queries.GetDeviceSummary;

public record GetDeviceSummaryQuery : IRequest<DeviceSummaryResponseDto>
{
    public Guid DeviceId { get; init; }
}
