using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Dashboard.Queries.GetGridPerformanceSummary;

public record GetGridPerformanceSummaryQuery : IRequest<GridPerformanceSummaryDto>
{
    public Guid DeviceId { get; init; }
}

public class GetGridPerformanceSummaryQueryHandler : IRequestHandler<GetGridPerformanceSummaryQuery, GridPerformanceSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly IDeviceMappingService _deviceMapping;
    private readonly IDemoDataService _demoDataService;
    private readonly IDemoSimulationDataService _demoSimulationService;

    public GetGridPerformanceSummaryQueryHandler(
        IApplicationDbContext context,
        ISender sender,
        IDeviceMappingService deviceMapping,
        IDemoDataService demoDataService,
        IDemoSimulationDataService demoSimulationService)
    {
        _context = context;
        _sender = sender;
        _deviceMapping = deviceMapping;
        _demoDataService = demoDataService;
        _demoSimulationService = demoSimulationService;
    }

    public async Task<GridPerformanceSummaryDto> Handle(GetGridPerformanceSummaryQuery request, CancellationToken cancellationToken)
    {
        // Check device access
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Device access denied");
        }

        // Check if this is a demo device
        var isDemoDevice = await _demoDataService.IsDemoDeviceAsync(request.DeviceId, cancellationToken);

        GridEnergyPeriodDto monthData;
        GridEnergyPeriodDto yearData;
        GridEnergyPeriodDto lifetimeData;

        if (isDemoDevice)
        {
            // Use simulated data for demo devices
            monthData = GetDemoMonthGridEnergy();
            yearData = GetDemoYearGridEnergy();
            lifetimeData = GetDemoLifetimeGridEnergy();
        }
        else
        {
            // Map local DeviceId to ThingsBoardDeviceId for real devices
            var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(request.DeviceId, cancellationToken);
            if (thingsBoardDeviceId == null)
            {
                throw new ArgumentException($"Device with ID {request.DeviceId} not found or has no ThingsBoard mapping.");
            }

            // Get data from database for real devices
            monthData = await GetMonthGridEnergyAsync(thingsBoardDeviceId.Value, cancellationToken);
            yearData = await GetYearGridEnergyAsync(thingsBoardDeviceId.Value, cancellationToken);
            lifetimeData = await GetLifetimeGridEnergyAsync(thingsBoardDeviceId.Value, cancellationToken);
        }

        return new GridPerformanceSummaryDto
        {
            Month = monthData,
            Year = yearData,
            Lifetime = lifetimeData
        };
    }

    #region Demo Data Methods

    /// <summary>
    /// Get simulated grid energy for current month (demo devices)
    /// </summary>
    private GridEnergyPeriodDto GetDemoMonthGridEnergy()
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        double totalImport = 0;
        double totalExport = 0;

        for (var date = monthStart; date <= today; date = date.AddDays(1))
        {
            var dailyEnergy = _demoSimulationService.GetDailyEnergy(date);
            totalImport += dailyEnergy.GridImportEnergyKWh;
            totalExport += dailyEnergy.GridExportEnergyKWh;
        }

        return new GridEnergyPeriodDto
        {
            GridImport = Math.Round(totalImport, 2),
            GridExport = Math.Round(totalExport, 2)
        };
    }

    /// <summary>
    /// Get simulated grid energy for current year (demo devices)
    /// </summary>
    private GridEnergyPeriodDto GetDemoYearGridEnergy()
    {
        var today = DateTime.UtcNow.Date;
        var yearStart = new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        double totalImport = 0;
        double totalExport = 0;

        for (var date = yearStart; date <= today; date = date.AddDays(1))
        {
            var dailyEnergy = _demoSimulationService.GetDailyEnergy(date);
            totalImport += dailyEnergy.GridImportEnergyKWh;
            totalExport += dailyEnergy.GridExportEnergyKWh;
        }

        return new GridEnergyPeriodDto
        {
            GridImport = Math.Round(totalImport, 2),
            GridExport = Math.Round(totalExport, 2)
        };
    }

    /// <summary>
    /// Get simulated lifetime grid energy (demo devices)
    /// Simulates ~2 years of historical data
    /// </summary>
    private GridEnergyPeriodDto GetDemoLifetimeGridEnergy()
    {
        var today = DateTime.UtcNow.Date;
        // Simulate 2 years of data (730 days)
        var lifetimeStart = today.AddDays(-730);

        double totalImport = 0;
        double totalExport = 0;

        for (var date = lifetimeStart; date <= today; date = date.AddDays(1))
        {
            var dailyEnergy = _demoSimulationService.GetDailyEnergy(date);
            totalImport += dailyEnergy.GridImportEnergyKWh;
            totalExport += dailyEnergy.GridExportEnergyKWh;
        }

        return new GridEnergyPeriodDto
        {
            GridImport = Math.Round(totalImport, 2),
            GridExport = Math.Round(totalExport, 2)
        };
    }

    #endregion

    /// <summary>
    /// Get grid energy for current month from DailyCumulativeEnergy records
    /// </summary>
    private async Task<GridEnergyPeriodDto> GetMonthGridEnergyAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var monthlyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DeviceLocalTime >= monthStart &&
                       x.DeviceLocalTime < monthEnd &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .ToListAsync(cancellationToken);

        // Group by date and take the most recent entry for each day to handle duplicates
        var latestDailyData = monthlyData
            .GroupBy(d => d.DeviceLocalTime.Date)
            .Select(g => g.OrderByDescending(d => d.DeviceLocalTime).First())
            .ToList();

        var totalImport = latestDailyData.Sum(x => x.GridImportEnergyKWh ?? 0);
        var totalExport = latestDailyData.Sum(x => x.GridExportEnergyKWh ?? 0);

        return new GridEnergyPeriodDto
        {
            GridImport = Math.Round(totalImport, 2),
            GridExport = Math.Round(totalExport, 2)
        };
    }

    /// <summary>
    /// Get grid energy for current year from DailyCumulativeEnergy records
    /// </summary>
    private async Task<GridEnergyPeriodDto> GetYearGridEnergyAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var yearStart = new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearEnd = yearStart.AddYears(1);

        var yearlyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DeviceLocalTime >= yearStart &&
                       x.DeviceLocalTime < yearEnd &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .ToListAsync(cancellationToken);

        // Group by date and take the most recent entry for each day to handle duplicates
        var latestDailyData = yearlyData
            .GroupBy(d => d.DeviceLocalTime.Date)
            .Select(g => g.OrderByDescending(d => d.DeviceLocalTime).First())
            .ToList();

        var totalImport = latestDailyData.Sum(x => x.GridImportEnergyKWh ?? 0);
        var totalExport = latestDailyData.Sum(x => x.GridExportEnergyKWh ?? 0);

        return new GridEnergyPeriodDto
        {
            GridImport = Math.Round(totalImport, 2),
            GridExport = Math.Round(totalExport, 2)
        };
    }

    /// <summary>
    /// Get lifetime grid energy by summing all DailyCumulativeEnergy records
    /// </summary>
    private async Task<GridEnergyPeriodDto> GetLifetimeGridEnergyAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken)
    {
        // Get ALL daily cumulative energy records for this device
        var allDailyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .ToListAsync(cancellationToken);

        // Group by date and take the most recent entry for each day to handle duplicates
        var latestDailyData = allDailyData
            .GroupBy(d => d.DeviceLocalTime.Date)
            .Select(g => g.OrderByDescending(d => d.DeviceLocalTime).First())
            .ToList();

        var totalImport = latestDailyData.Sum(x => x.GridImportEnergyKWh ?? 0);
        var totalExport = latestDailyData.Sum(x => x.GridExportEnergyKWh ?? 0);

        return new GridEnergyPeriodDto
        {
            GridImport = Math.Round(totalImport, 2),
            GridExport = Math.Round(totalExport, 2)
        };
    }
}
