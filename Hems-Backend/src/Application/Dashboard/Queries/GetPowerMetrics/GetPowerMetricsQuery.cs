using Hems_Backend.Application.Common.Models;
using MediatR;

namespace Hems_Backend.Application.Dashboard.Queries.GetPowerMetrics;

public record GetPowerMetricsQuery : IRequest<PowerMetricsResponseDto>
{
    public Guid DeviceId { get; init; }
    public string Period { get; init; } = string.Empty; // daily, weekly, monthly, yearly
}
