using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Dashboard.Queries.GetSoc;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;

namespace Hems_Backend.Application.Dashboard.Queries.GetTodayEnergySummary;

public class GetDailyEnergySummaryHandler : IRequestHandler<GetDailyEnergySummaryQuery, DailyEnergySummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly IDeviceMappingService _deviceMapping;

    public GetDailyEnergySummaryHandler(IApplicationDbContext context, ISender sender, IDeviceMappingService deviceMapping)
    {
        _context = context;
        _sender = sender;
        _deviceMapping = deviceMapping;
    }

    public async Task<DailyEnergySummaryDto> Handle(GetDailyEnergySummaryQuery request, CancellationToken cancellationToken)
    {

        // Device access check (uses local DeviceId)
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Device access denied");
        }

        // Map local DeviceId to ThingsBoardDeviceId for querying telemetry data
        var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(request.DeviceId, cancellationToken);
        if (thingsBoardDeviceId == null)
        {
            throw new ArgumentException($"Device with ID {request.DeviceId} not found or has no ThingsBoard mapping.");
        }

        // Query the latest BatterySoc data for the device using ThingsBoardDeviceId
        var LatestDailyCumulativeEnergyData = await _context.DeviceEnergyData
            .Where(dd => dd.ThingsBoardDeviceId == thingsBoardDeviceId && dd.DataType == "DailyCumulativeEnergyData")
            .OrderByDescending(dd => dd.Created)
            .FirstOrDefaultAsync(cancellationToken);

        if (LatestDailyCumulativeEnergyData == null)
        {
            // Return empty response if no data found
            return new DailyEnergySummaryDto
            {
                DataDateTime = DateTime.UtcNow,
                DeviceId = request.DeviceId,
                GridImport =0.0,
                GridExport =0.0,
                Consumption =0.0,
                Production =0.0
            };
        }       

        return new DailyEnergySummaryDto
        {
            DataDateTime = DateTime.UtcNow,
            DeviceId = request.DeviceId,
            GridImport = LatestDailyCumulativeEnergyData.GridImportEnergyKWh,
            GridExport = LatestDailyCumulativeEnergyData.GridExportEnergyKWh,
            Consumption = LatestDailyCumulativeEnergyData.LoadEnergyKWh + LatestDailyCumulativeEnergyData.BatteryChargeEnergyKWh,
            Production = LatestDailyCumulativeEnergyData.PvTotalEnergyKWh + LatestDailyCumulativeEnergyData.GeneratorEnergyKWh
        };
    }
}
