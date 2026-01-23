using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EnergySourceEnum = Hems_Backend.Domain.Enums.EnergySource;

namespace Hems_Backend.Application.Dashboard.Queries.GetEnergySourceMix;

/// <summary>
/// Handler for GetEnergySourceMixQuery.
/// Calculates energy source mix using proportional attribution algorithm.
/// Handles variable sampling intervals by calculating actual time deltas between samples.
/// </summary>
public class GetEnergySourceMixQueryHandler
    : IRequestHandler<GetEnergySourceMixQuery, EnergySourceMixDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly IDeviceMappingService _deviceMapping;
    private readonly IDemoDataService _demoDataService;

    // Default expected interval (used for the last sample where we can't calculate delta)
    private const double DefaultIntervalMinutes = 5.0;

    // Maximum allowed interval to prevent outliers from skewing results
    private const double MaxIntervalMinutes = 15.0;

    public GetEnergySourceMixQueryHandler(
        IApplicationDbContext context,
        ISender sender,
        IDeviceMappingService deviceMapping,
        IDemoDataService demoDataService)
    {
        _context = context;
        _sender = sender;
        _deviceMapping = deviceMapping;
        _demoDataService = demoDataService;
    }

    public async Task<EnergySourceMixDto> Handle(
        GetEnergySourceMixQuery request,
        CancellationToken cancellationToken)
    {
        // Step 1: Device access check (uses local DeviceId)
        var hasAccess = await _sender.Send(
            new CheckDeviceAccessQuery { DeviceId = request.DeviceId },
            cancellationToken);

        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Device access denied");
        }

        // Step 2: Check if this is a demo device - return simulated data
        var isDemoDevice = await _demoDataService.IsDemoDeviceAsync(request.DeviceId, cancellationToken);
        if (isDemoDevice)
        {
            return GetSimulatedEnergyMix(request.DeviceId, request.Date);
        }

        // Step 3: Map local DeviceId to ThingsBoardDeviceId
        var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(
            request.DeviceId,
            cancellationToken);

        if (thingsBoardDeviceId == null)
        {
            throw new ArgumentException($"Device with ID {request.DeviceId} not found or has no ThingsBoard mapping.");
        }

        // Step 4: Calculate start and end of day in device local time
        var startOfDay = new DateTime(request.Date.Year, request.Date.Month, request.Date.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var endOfDay = startOfDay.AddDays(1);

        // Step 5: Query power data for the day
        var powerData = await _context.DevicePowerData
            .Where(pd => pd.ThingsBoardDeviceId == thingsBoardDeviceId.Value
                      && pd.DataType == DataTypes.PowerFlow
                      && pd.DeviceLocalTime >= startOfDay
                      && pd.DeviceLocalTime < endOfDay)
            .OrderBy(pd => pd.DeviceLocalTime)
            .ToListAsync(cancellationToken);

        // Step 6: Calculate energy mix using proportional attribution with variable intervals
        var distribution = CalculateProportionalAttribution(powerData);

        // Step 7: Build response
        return BuildResponse(request.DeviceId, request.Date, distribution, powerData);
    }

    /// <summary>
    /// Calculate energy mix using proportional attribution algorithm.
    /// Uses actual time intervals between samples for accurate energy conversion.
    ///
    /// For each time sample:
    /// 1. Calculate the time interval to the NEXT sample (or use default for last sample)
    /// 2. Separate SOURCES (only positive values):
    ///    - PV (solar generation)
    ///    - Battery discharge (positive BatteryPowerW)
    ///    - Grid import (positive GridTotalPowerW)
    ///    - Generator
    ///
    /// 3. Calculate proportional contribution to load:
    ///    Source_to_Load = Load × (Source / Total_Supply)
    ///
    /// 4. Convert power to energy using actual interval:
    ///    Energy_Wh = Power_W × (Interval_Minutes / 60)
    /// </summary>
    private EnergyMixResult CalculateProportionalAttribution(
        List<Domain.Entities.DevicePowerData> powerData)
    {
        var result = new EnergyMixResult();

        if (powerData.Count == 0)
        {
            return result;
        }

        for (int i = 0; i < powerData.Count; i++)
        {
            var sample = powerData[i];

            // Calculate interval to next sample (or use default for last sample)
            double intervalMinutes;
            if (i < powerData.Count - 1)
            {
                var nextSample = powerData[i + 1];
                intervalMinutes = (nextSample.DeviceLocalTime - sample.DeviceLocalTime).TotalMinutes;

                // Cap interval to prevent outliers (e.g., large gaps due to device offline)
                intervalMinutes = Math.Min(intervalMinutes, MaxIntervalMinutes);

                // Ensure positive interval (handle any timestamp ordering issues)
                intervalMinutes = Math.Max(intervalMinutes, 0);
            }
            else
            {
                // Last sample - use default interval
                intervalMinutes = DefaultIntervalMinutes;
            }

            // Skip samples with zero or negative interval
            if (intervalMinutes <= 0)
            {
                continue;
            }

            // Track total time span
            result.TotalIntervalMinutes += intervalMinutes;

            // Extract power values with null handling
            var pvPower = sample.PvTotalPowerW ?? 0;
            var generatorPower = sample.GeneratorTotalPowerW ?? 0;
            var gridPower = sample.GridTotalPowerW ?? 0;
            var batteryPower = sample.BatteryPowerW ?? 0;
            var loadPower = sample.LoadTotalPowerW ?? 0;

            // Separate sources (only positive contributions)
            var pvSupply = Math.Max(pvPower, 0);
            var generatorSupply = Math.Max(generatorPower, 0);
            var gridImport = Math.Max(gridPower, 0);           // positive = import
            var batteryDischarge = Math.Max(batteryPower, 0);  // positive = discharge

            // Calculate total supply to the system
            var totalSupply = pvSupply + generatorSupply + gridImport + batteryDischarge;

            // Skip if no supply or no load (edge case)
            if (totalSupply <= 0 || loadPower <= 0)
            {
                if (loadPower > 0 && totalSupply <= 0)
                {
                    result.ZeroSupplySamples++;
                }
                continue;
            }

            // Calculate load fraction from each source using proportional attribution
            // Formula: Source_to_Load = Load × (Source / Total_Supply)
            var pvToLoad = loadPower * (pvSupply / totalSupply);
            var generatorToLoad = loadPower * (generatorSupply / totalSupply);
            var gridToLoad = loadPower * (gridImport / totalSupply);
            var batteryToLoad = loadPower * (batteryDischarge / totalSupply);

            // Convert power (W) to energy (Wh) using actual interval
            // Energy = Power × Time, where time is in hours
            var hoursInterval = intervalMinutes / 60.0;

            result.SolarEnergyWh += pvToLoad * hoursInterval;
            result.GeneratorEnergyWh += generatorToLoad * hoursInterval;
            result.GridEnergyWh += gridToLoad * hoursInterval;
            result.BatteryEnergyWh += batteryToLoad * hoursInterval;
            result.TotalLoadEnergyWh += loadPower * hoursInterval;

            result.SampleCount++;
        }

        return result;
    }

    /// <summary>
    /// Build the response DTO from calculated energy mix
    /// </summary>
    private EnergySourceMixDto BuildResponse(
        Guid deviceId,
        DateOnly date,
        EnergyMixResult distribution,
        List<Domain.Entities.DevicePowerData> powerData)
    {
        var totalLoadWh = distribution.TotalLoadEnergyWh;
        var solarWh = distribution.SolarEnergyWh;
        var batteryWh = distribution.BatteryEnergyWh;
        var gridWh = distribution.GridEnergyWh;
        var generatorWh = distribution.GeneratorEnergyWh;

        // Calculate percentages (avoid division by zero)
        var solarContribution = totalLoadWh > 0 ? (solarWh / totalLoadWh) * 100 : 0;
        var batteryContribution = totalLoadWh > 0 ? (batteryWh / totalLoadWh) * 100 : 0;
        var gridContribution = totalLoadWh > 0 ? (gridWh / totalLoadWh) * 100 : 0;
        var generatorContribution = totalLoadWh > 0 ? (generatorWh / totalLoadWh) * 100 : 0;

        // Build sources list using enum for internal logic, string for output
        var sources = new List<EnergySourceContribution>();

        // Only include sources that contributed energy
        if (solarWh > 0)
        {
            sources.Add(new EnergySourceContribution
            {
                Source = EnergySourceEnum.Solar.ToString(),
                ProvidedEnergyWh = Math.Round(solarWh, 2),
                Contribution = Math.Round(solarContribution, 1)
            });
        }

        if (batteryWh > 0)
        {
            sources.Add(new EnergySourceContribution
            {
                Source = EnergySourceEnum.Battery.ToString(),
                ProvidedEnergyWh = Math.Round(batteryWh, 2),
                Contribution = Math.Round(batteryContribution, 1)
            });
        }

        if (gridWh > 0)
        {
            sources.Add(new EnergySourceContribution
            {
                Source = EnergySourceEnum.Grid.ToString(),
                ProvidedEnergyWh = Math.Round(gridWh, 2),
                Contribution = Math.Round(gridContribution, 1)
            });
        }

        if (generatorWh > 0)
        {
            sources.Add(new EnergySourceContribution
            {
                Source = EnergySourceEnum.Generator.ToString(),
                ProvidedEnergyWh = Math.Round(generatorWh, 2),
                Contribution = Math.Round(generatorContribution, 1)
            });
        }

        // Sort by contribution descending (largest contributor first)
        sources = sources.OrderByDescending(s => s.Contribution).ToList();

        // Calculate average interval
        var avgInterval = distribution.SampleCount > 0
            ? distribution.TotalIntervalMinutes / distribution.SampleCount
            : 0;

        // Build summary
        var summary = new EnergySourceMixSummary
        {
            SampleCount = distribution.SampleCount,
            FirstSampleTime = powerData.FirstOrDefault()?.DeviceLocalTime.ToString("HH:mm"),
            LastSampleTime = powerData.LastOrDefault()?.DeviceLocalTime.ToString("HH:mm"),
            AverageIntervalMinutes = Math.Round(avgInterval, 2),
            TotalTimeSpanMinutes = Math.Round(distribution.TotalIntervalMinutes, 2),
            ZeroSupplySamples = distribution.ZeroSupplySamples
        };

        return new EnergySourceMixDto
        {
            Date = date.ToString("yyyy-MM-dd"),
            DeviceId = deviceId,
            LoadDemandWh = Math.Round(totalLoadWh, 2),
            Sources = sources,
            Summary = summary
        };
    }

    /// <summary>
    /// Generate simulated energy mix for demo devices
    /// </summary>
    private static EnergySourceMixDto GetSimulatedEnergyMix(Guid deviceId, DateOnly date)
    {
        var random = new Random(date.DayNumber); // Seed for consistent results per day

        // Generate realistic distribution (values in Wh)
        var totalLoadWh = (15.0 + random.NextDouble() * 10) * 1000; // 15-25 kWh = 15000-25000 Wh
        var solarContribution = 40 + random.NextDouble() * 20; // 40-60%
        var batteryContribution = 15 + random.NextDouble() * 15; // 15-30%
        var gridContribution = 100 - solarContribution - batteryContribution - (random.NextDouble() * 5);
        var generatorContribution = Math.Max(0, 100 - solarContribution - batteryContribution - gridContribution);

        // Normalize to 100%
        var total = solarContribution + batteryContribution + gridContribution + generatorContribution;
        solarContribution = (solarContribution / total) * 100;
        batteryContribution = (batteryContribution / total) * 100;
        gridContribution = (gridContribution / total) * 100;
        generatorContribution = (generatorContribution / total) * 100;

        var sources = new List<EnergySourceContribution>
        {
            new()
            {
                Source = EnergySourceEnum.Solar.ToString(),
                ProvidedEnergyWh = Math.Round(totalLoadWh * solarContribution / 100, 2),
                Contribution = Math.Round(solarContribution, 1)
            },
            new()
            {
                Source = EnergySourceEnum.Battery.ToString(),
                ProvidedEnergyWh = Math.Round(totalLoadWh * batteryContribution / 100, 2),
                Contribution = Math.Round(batteryContribution, 1)
            },
            new()
            {
                Source = EnergySourceEnum.Grid.ToString(),
                ProvidedEnergyWh = Math.Round(totalLoadWh * gridContribution / 100, 2),
                Contribution = Math.Round(gridContribution, 1)
            }
        };

        // Occasionally add generator
        if (generatorContribution > 1)
        {
            sources.Add(new EnergySourceContribution
            {
                Source = EnergySourceEnum.Generator.ToString(),
                ProvidedEnergyWh = Math.Round(totalLoadWh * generatorContribution / 100, 2),
                Contribution = Math.Round(generatorContribution, 1)
            });
        }

        // Sort by contribution descending
        sources = sources.OrderByDescending(s => s.Contribution).ToList();

        return new EnergySourceMixDto
        {
            Date = date.ToString("yyyy-MM-dd"),
            DeviceId = deviceId,
            LoadDemandWh = Math.Round(totalLoadWh, 2),
            Sources = sources,
            Summary = new EnergySourceMixSummary
            {
                SampleCount = 288, // 24 hours × 12 samples/hour
                FirstSampleTime = "00:00",
                LastSampleTime = "23:55",
                AverageIntervalMinutes = 5.0,
                TotalTimeSpanMinutes = 1440, // 24 hours
                ZeroSupplySamples = 0
            }
        };
    }

    /// <summary>
    /// Internal class to hold intermediate calculation results
    /// </summary>
    private class EnergyMixResult
    {
        public double SolarEnergyWh { get; set; }
        public double BatteryEnergyWh { get; set; }
        public double GridEnergyWh { get; set; }
        public double GeneratorEnergyWh { get; set; }
        public double TotalLoadEnergyWh { get; set; }
        public double TotalIntervalMinutes { get; set; }
        public int SampleCount { get; set; }
        public int ZeroSupplySamples { get; set; }
    }
}
