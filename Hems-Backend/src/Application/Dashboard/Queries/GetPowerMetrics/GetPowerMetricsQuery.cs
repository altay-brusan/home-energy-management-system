using MediatR;

namespace Hems_Backend.Application.Dashboard.Queries.GetPowerMetrics;

public record GetPowerMetricsQuery : IRequest<PowerMetricsDto>
{
    public Guid DeviceId { get; init; }
    public string Period { get; init; } = string.Empty; // daily, weekly, monthly, yearly
}
