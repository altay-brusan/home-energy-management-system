using Hems_Backend.Application.Common.Models;
using MediatR;

namespace Hems_Backend.Application.Dashboard.Queries.GetPowerMetrics;

public record GetPowerAndEnergyMetricsQuery : IRequest<PowerAndEnergyMetricsResponseDto>
{
    public Guid DeviceId { get; init; }
    public string Period { get; init; } = string.Empty; // day, week, month, year

    // For period=day: ISO date format (e.g., 2025-12-03)
    public DateOnly? Date { get; init; }

    // For period=week: year and week number (e.g., year=2025&week=49)
    public int? Year { get; init; }
    public int? Week { get; init; }

    // For period=month: year and month (e.g., year=2025&month=12)
    public int? Month { get; init; }

    // For period=year: just year (e.g., year=2025)
    // Year property is already defined above and shared with week/month
}
