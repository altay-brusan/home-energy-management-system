using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Dashboard.Queries.GetGridPerformance;

public record GetGridPerformanceQuery : IRequest<GridPerformanceResponseDto>
{
    public Guid DeviceId { get; init; }
    public string Period { get; init; } = string.Empty; // "monthly", "yearly", "custom"
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
}

public class GetGridPerformanceQueryHandler : IRequestHandler<GetGridPerformanceQuery, GridPerformanceResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly IDeviceMappingService _deviceMapping;

    public GetGridPerformanceQueryHandler(
        IApplicationDbContext context,
        ISender sender,
        IDeviceMappingService deviceMapping)
    {
        _context = context;
        _sender = sender;
        _deviceMapping = deviceMapping;
    }

    public async Task<GridPerformanceResponseDto> Handle(GetGridPerformanceQuery request, CancellationToken cancellationToken)
    {
        // Check device access
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Device access denied");
        }

        // Map local DeviceId to ThingsBoardDeviceId
        var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(request.DeviceId, cancellationToken);
        if (thingsBoardDeviceId == null)
        {
            throw new ArgumentException($"Device with ID {request.DeviceId} not found or has no ThingsBoard mapping.");
        }

        // Validate period
        var period = request.Period.ToLower();
        if (period != "monthly" && period != "yearly" && period != "custom")
        {
            throw new ArgumentException("Period must be 'monthly', 'yearly', or 'custom'");
        }

        // Validate custom range
        if (period == "custom" && (!request.StartDate.HasValue || !request.EndDate.HasValue))
        {
            throw new ArgumentException("StartDate and EndDate are required for custom period");
        }

        List<GridPerformanceDataPoint> dataPoints;

        switch (period)
        {
            case "monthly":
                dataPoints = await GetMonthlyDataAsync(thingsBoardDeviceId.Value, cancellationToken);
                break;
            case "yearly":
                dataPoints = await GetYearlyDataAsync(thingsBoardDeviceId.Value, cancellationToken);
                break;
            case "custom":
                dataPoints = await GetCustomRangeDataAsync(thingsBoardDeviceId.Value, request.StartDate!.Value, request.EndDate!.Value, cancellationToken);
                break;
            default:
                throw new ArgumentException($"Invalid period: {period}");
        }

        // Calculate totals
        var totalImport = dataPoints.Sum(d => d.GridImport);
        var totalExport = dataPoints.Sum(d => d.GridExport);

        return new GridPerformanceResponseDto
        {
            Period = period,
            Data = dataPoints,
            Total = new GridPerformanceTotal
            {
                TotalGridImport = Math.Round(totalImport, 2),
                TotalGridExport = Math.Round(totalExport, 2)
            }
        };
    }

    /// <summary>
    /// Get daily grid data for the current month
    /// </summary>
    private async Task<List<GridPerformanceDataPoint>> GetMonthlyDataAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var dailyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DeviceLocalTime >= monthStart &&
                       x.DeviceLocalTime < monthEnd &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .OrderBy(x => x.DeviceLocalTime)
            .ToListAsync(cancellationToken);

        // Group by date and take the most recent entry for each day
        var latestDailyData = dailyData
            .GroupBy(d => d.DeviceLocalTime.Date)
            .Select(g => g.OrderByDescending(d => d.DeviceLocalTime).First())
            .OrderBy(d => d.DeviceLocalTime);

        return latestDailyData.Select(d => new GridPerformanceDataPoint
        {
            Timestamp = d.DeviceLocalTime.ToString("yyyy-MM-dd"),
            GridImport = Math.Round(d.GridImportEnergyKWh ?? 0, 2),
            GridExport = Math.Round(d.GridExportEnergyKWh ?? 0, 2)
        }).ToList();
    }

    /// <summary>
    /// Get monthly aggregated grid data for the current year
    /// </summary>
    private async Task<List<GridPerformanceDataPoint>> GetYearlyDataAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var yearStart = new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearEnd = yearStart.AddYears(1);

        var dailyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DeviceLocalTime >= yearStart &&
                       x.DeviceLocalTime < yearEnd &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .OrderBy(x => x.DeviceLocalTime)
            .ToListAsync(cancellationToken);

        // Group by date and take the most recent entry for each day
        var latestDailyData = dailyData
            .GroupBy(d => d.DeviceLocalTime.Date)
            .Select(g => g.OrderByDescending(d => d.DeviceLocalTime).First());

        // Group by month and sum the values
        var monthlyData = latestDailyData
            .GroupBy(d => new { d.DeviceLocalTime.Year, d.DeviceLocalTime.Month })
            .Select(g => new GridPerformanceDataPoint
            {
                Timestamp = $"{g.Key.Year}-{g.Key.Month:D2}",
                GridImport = Math.Round(g.Sum(d => d.GridImportEnergyKWh ?? 0), 2),
                GridExport = Math.Round(g.Sum(d => d.GridExportEnergyKWh ?? 0), 2)
            })
            .OrderBy(d => d.Timestamp)
            .ToList();

        return monthlyData;
    }

    /// <summary>
    /// Get daily grid data for a custom date range
    /// </summary>
    private async Task<List<GridPerformanceDataPoint>> GetCustomRangeDataAsync(
        Guid thingsBoardDeviceId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        // Ensure dates are in UTC and normalized to start of day
        var rangeStart = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc);
        var rangeEnd = DateTime.SpecifyKind(endDate.Date.AddDays(1), DateTimeKind.Utc); // Include the end date

        var dailyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DeviceLocalTime >= rangeStart &&
                       x.DeviceLocalTime < rangeEnd &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .OrderBy(x => x.DeviceLocalTime)
            .ToListAsync(cancellationToken);

        // Group by date and take the most recent entry for each day
        var latestDailyData = dailyData
            .GroupBy(d => d.DeviceLocalTime.Date)
            .Select(g => g.OrderByDescending(d => d.DeviceLocalTime).First())
            .OrderBy(d => d.DeviceLocalTime).ToList();

        return latestDailyData.Select(d => new GridPerformanceDataPoint
        {
            Timestamp = d.DeviceLocalTime.ToString("yyyy-MM-dd"),
            GridImport = Math.Round(d.GridImportEnergyKWh ?? 0, 2),
            GridExport = Math.Round(d.GridExportEnergyKWh ?? 0, 2)
        }).ToList();
    }
}
