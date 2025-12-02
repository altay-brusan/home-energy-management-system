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

    public GetGridPerformanceSummaryQueryHandler(
        IApplicationDbContext context,
        ISender sender,
        IDeviceMappingService deviceMapping)
    {
        _context = context;
        _sender = sender;
        _deviceMapping = deviceMapping;
    }

    public async Task<GridPerformanceSummaryDto> Handle(GetGridPerformanceSummaryQuery request, CancellationToken cancellationToken)
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

        // Get month data
        var monthData = await GetMonthGridEnergyAsync(thingsBoardDeviceId.Value, cancellationToken);

        // Get year data
        var yearData = await GetYearGridEnergyAsync(thingsBoardDeviceId.Value, cancellationToken);

        // Get lifetime data
        var lifetimeData = await GetLifetimeGridEnergyAsync(thingsBoardDeviceId.Value, cancellationToken);

        return new GridPerformanceSummaryDto
        {
            Month = monthData,
            Year = yearData,
            Lifetime = lifetimeData
        };
    }

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
                       x.DataDateTime >= monthStart &&
                       x.DataDateTime < monthEnd &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .ToListAsync(cancellationToken);

        var totalImport = monthlyData.Sum(x => x.GridImportEnergyKWh ?? 0);
        var totalExport = monthlyData.Sum(x => x.GridExportEnergyKWh ?? 0);

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
                       x.DataDateTime >= yearStart &&
                       x.DataDateTime < yearEnd &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .ToListAsync(cancellationToken);

        var totalImport = yearlyData.Sum(x => x.GridImportEnergyKWh ?? 0);
        var totalExport = yearlyData.Sum(x => x.GridExportEnergyKWh ?? 0);

        return new GridEnergyPeriodDto
        {
            GridImport = Math.Round(totalImport, 2),
            GridExport = Math.Round(totalExport, 2)
        };
    }

    /// <summary>
    /// Get lifetime grid energy from most recent LifetimeEnergy record
    /// </summary>
    private async Task<GridEnergyPeriodDto> GetLifetimeGridEnergyAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken)
    {
        // Get the most recent lifetime energy record
        var lifetimeRecord = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DataType == DataTypes.LifetimeEnergy)
            .OrderByDescending(x => x.DataDateTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (lifetimeRecord == null)
        {
            // No lifetime data available, return zeros
            return new GridEnergyPeriodDto
            {
                GridImport = 0,
                GridExport = 0
            };
        }

        var totalImport = lifetimeRecord.GridImportEnergyKWh ?? 0;
        var totalExport = lifetimeRecord.GridExportEnergyKWh ?? 0;

        return new GridEnergyPeriodDto
        {
            GridImport = Math.Round(totalImport, 2),
            GridExport = Math.Round(totalExport, 2)
        };
    }
}
