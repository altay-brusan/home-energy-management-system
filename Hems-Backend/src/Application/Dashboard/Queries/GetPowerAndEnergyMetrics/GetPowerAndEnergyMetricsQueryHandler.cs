using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Hems_Backend.Application.Dashboard.Queries.GetPowerMetrics;

public class GetPowerAndEnergyMetricsQueryHandler : IRequestHandler<GetPowerAndEnergyMetricsQuery, PowerAndEnergyMetricsResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly IDeviceMappingService _deviceMapping;
    private readonly ITimezoneService _timezoneService;
    private readonly IDemoDataService _demoDataService;
    private readonly IDemoSimulationDataService _demoSimulationService;

    public GetPowerAndEnergyMetricsQueryHandler(
        IApplicationDbContext context,
        ISender sender,
        IDeviceMappingService deviceMapping,
        ITimezoneService timezoneService,
        IDemoDataService demoDataService,
        IDemoSimulationDataService demoSimulationService)
    {
        _context = context;
        _sender = sender;
        _deviceMapping = deviceMapping;
        _timezoneService = timezoneService;
        _demoDataService = demoDataService;
        _demoSimulationService = demoSimulationService;
    }

    public async Task<PowerAndEnergyMetricsResponseDto> Handle(GetPowerAndEnergyMetricsQuery request, CancellationToken cancellationToken)
    {
        // Device access kontrolü (uses local DeviceId)
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Device access denied");
        }

        // Check if this is a demo device - return simulated data
        var isDemoDevice = await _demoDataService.IsDemoDeviceAsync(request.DeviceId, cancellationToken);
        if (isDemoDevice)
        {
            return request.Period.ToLower() switch
            {
                "day" => GetSimulatedDailyMetrics(request.Date!.Value),
                "week" => GetSimulatedWeeklyMetrics(request.Year!.Value, request.Week!.Value),
                "month" => GetSimulatedMonthlyMetrics(request.Year!.Value, request.Month!.Value),
                "year" => GetSimulatedYearlyMetrics(request.Year!.Value),
                _ => throw new ArgumentException($"Invalid period: {request.Period}. Must be one of: day, week, month, year")
            };
        }

        // Map local DeviceId to ThingsBoardDeviceId
        var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(request.DeviceId, cancellationToken);
        if (thingsBoardDeviceId == null)
        {
            throw new ArgumentException($"Device with ID {request.DeviceId} not found or has no ThingsBoard mapping.");
        }

        return request.Period.ToLower() switch
        {
            "day" => await GetDailyMetricsAsync(request.DeviceId, thingsBoardDeviceId.Value, request.Date!.Value, cancellationToken),
            "week" => await GetWeeklyMetricsAsync(request.DeviceId, thingsBoardDeviceId.Value, request.Year!.Value, request.Week!.Value, cancellationToken),
            "month" => await GetMonthlyMetricsAsync(request.DeviceId, thingsBoardDeviceId.Value, request.Year!.Value, request.Month!.Value, cancellationToken),
            "year" => await GetYearlyMetricsAsync(request.DeviceId, thingsBoardDeviceId.Value, request.Year!.Value, cancellationToken),
            _ => throw new ArgumentException($"Invalid period: {request.Period}. Must be one of: day, week, month, year")
        };
    }

    /// <summary>
    /// Daily metrics: Return power data from 5-minute samples
    /// Uses DevicePowerData table which stores instantaneous power readings
    /// Interpolates power values for missing samples
    /// Uses device local timezone for accurate time-based queries
    /// </summary>
    private async Task<PowerAndEnergyMetricsResponseDto> GetDailyMetricsAsync(Guid deviceId, Guid thingsBoardDeviceId, DateOnly date, CancellationToken cancellationToken)
    {
        // Convert the provided date to device's local timezone
        var startOfDay = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var endOfDay = startOfDay.AddDays(1);

        // Get all power samples for today (5-min intervals) from DevicePowerData
        // DeviceLocalTime is in device local time, so we compare with device local time ranges
        var powerData = await _context.DevicePowerData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DeviceLocalTime >= startOfDay &&
                       x.DeviceLocalTime < endOfDay &&
                       x.DataType == DataTypes.PowerFlow)
            .OrderBy(x => x.DeviceLocalTime)
            .ToListAsync(cancellationToken);

        if (!powerData.Any())
        {
            return CreateEmptyResponse("Day");
        }

        // Apply power interpolation for missing data (if gaps > 5 minutes)
        var interpolatedPowerData = InterpolatePowerGaps(powerData, TimeSpan.FromMinutes(5));

        // Convert each power sample to a data point - ONLY power values, no energy
        var dataPoints = interpolatedPowerData
            .Select(sample => new PowerAndEnergyMetricsDataPoint
            {
                Timestamp = sample.DeviceLocalTime.ToString("HH:mm"),

                // Power values in Watts
                SolarPower = Math.Round(sample.PvTotalPowerW ?? 0, 2),
                GridPower = Math.Round(sample.GridTotalPowerW ?? 0, 2),
                BatteryPower = Math.Round(sample.BatteryPowerW ?? 0, 2),
                LoadPower = Math.Round(sample.LoadTotalPowerW ?? 0, 2),
                GeneratorPower = Math.Round(sample.GeneratorTotalPowerW ?? 0, 2),

                Unit = "W"
            })
            .ToList();

        return CreateResponse("Day", dataPoints);
    }

    /// <summary>
    /// Weekly metrics: Use daily cumulative energy data (end-of-day snapshots)
    /// Each day shows the total energy produced/consumed for that day from DailyCumulativeEnergy
    /// Interpolates energy values for missing days
    /// Uses device local timezone for accurate time-based queries
    /// </summary>
    private async Task<PowerAndEnergyMetricsResponseDto> GetWeeklyMetricsAsync(Guid deviceId, Guid thingsBoardDeviceId, int year, int week, CancellationToken cancellationToken)
    {
        // Calculate the start and end dates for the given ISO week
        var weekStart = ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
        var weekStartLocal = DateTime.SpecifyKind(weekStart, DateTimeKind.Unspecified);
        var weekEnd = weekStartLocal.AddDays(7);

        // Get all DailyCumulativeEnergy samples for the week (one sample per day)
        // DeviceLocalTime is in device local time
        var dailyEnergyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DeviceLocalTime >= weekStartLocal &&
                       x.DeviceLocalTime < weekEnd &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .OrderBy(x => x.DeviceLocalTime)
            .ToListAsync(cancellationToken);

        // Apply energy interpolation for missing days
        var interpolatedEnergyData = InterpolateEnergyGaps(dailyEnergyData, TimeSpan.FromDays(1));

        var dataPoints = new List<PowerAndEnergyMetricsDataPoint>();

        // Process each day of the week
        for (int i = 0; i < 7; i++)
        {
            var day = weekStart.AddDays(i);
            var dayStart = new DateTime(day.Year, day.Month, day.Day, 0, 0, 0, DateTimeKind.Unspecified);
            var dayEnd = dayStart.AddDays(1);

            // Find the daily cumulative energy for this day (after interpolation)
            var dayEnergy = interpolatedEnergyData.FirstOrDefault(x => x.DeviceLocalTime >= dayStart && x.DeviceLocalTime < dayEnd);

            if (dayEnergy != null)
            {
                dataPoints.Add(new PowerAndEnergyMetricsDataPoint
                {
                    Timestamp = day.ToString("yyyy-MM-dd"),

                    // Energy values in "Power" fields (kWh despite field names)
                    SolarPower = Math.Round(dayEnergy.PvTotalEnergyKWh ?? 0, 3),
                    LoadPower = Math.Round(dayEnergy.LoadEnergyKWh ?? 0, 3),
                    GeneratorPower = Math.Round(dayEnergy.GeneratorEnergyKWh ?? 0, 3),
                    GridPower = Math.Round((dayEnergy.GridImportEnergyKWh ?? 0) - (dayEnergy.GridExportEnergyKWh ?? 0), 3),
                    BatteryPower = Math.Round((dayEnergy.BatteryDischargeEnergyKWh ?? 0) - (dayEnergy.BatteryChargeEnergyKWh ?? 0), 3),

                    // Detailed energy breakdown
                    GridImportEnergyKWh = Math.Round(dayEnergy.GridImportEnergyKWh ?? 0, 3),
                    GridExportEnergyKWh = Math.Round(dayEnergy.GridExportEnergyKWh ?? 0, 3),
                    BatteryChargeEnergyKWh = Math.Round(dayEnergy.BatteryChargeEnergyKWh ?? 0, 3),
                    BatteryDischargeEnergyKWh = Math.Round(dayEnergy.BatteryDischargeEnergyKWh ?? 0, 3),
                    LoadEnergyKWh = Math.Round(dayEnergy.LoadEnergyKWh ?? 0, 3),
                    GeneratorEnergyKWh = Math.Round(dayEnergy.GeneratorEnergyKWh ?? 0, 3),

                    Unit = "kWh"
                });
            }
        }

        return CreateResponse("Week", dataPoints);
    }

    /// <summary>
    /// Monthly metrics: Use daily cumulative energy data (end-of-day snapshots)
    /// Each day shows the total energy produced/consumed for that day from DailyCumulativeEnergy
    /// Interpolates energy values for missing days
    /// Uses device local timezone for accurate time-based queries
    /// </summary>
    private async Task<PowerAndEnergyMetricsResponseDto> GetMonthlyMetricsAsync(Guid deviceId, Guid thingsBoardDeviceId, int year, int month, CancellationToken cancellationToken)
    {
        // Calculate the start and end dates for the given month
        var monthStart = new DateTime(year, month, 1);
        var monthStartLocal = DateTime.SpecifyKind(monthStart, DateTimeKind.Unspecified);
        var monthEnd = monthStart.AddMonths(1);

        // Get all DailyCumulativeEnergy samples for the month
        // DeviceLocalTime is in device local time
        var dailyEnergyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DeviceLocalTime >= monthStartLocal &&
                       x.DeviceLocalTime < monthEnd &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .OrderBy(x => x.DeviceLocalTime)
            .ToListAsync(cancellationToken);

        // Apply energy interpolation for missing days
        var interpolatedEnergyData = InterpolateEnergyGaps(dailyEnergyData, TimeSpan.FromDays(1));

        var dataPoints = new List<PowerAndEnergyMetricsDataPoint>();

        // Process each day of the month
        var daysInMonth = DateTime.DaysInMonth(year, month);
        for (int i = 1; i <= daysInMonth; i++)
        {
            var day = new DateTime(year, month, i);

            var dayStart = new DateTime(day.Year, day.Month, day.Day, 0, 0, 0, DateTimeKind.Unspecified);
            var dayEnd = dayStart.AddDays(1);

            // Find the daily cumulative energy for this day (after interpolation)
            var dayEnergy = interpolatedEnergyData.FirstOrDefault(x => x.DeviceLocalTime >= dayStart && x.DeviceLocalTime < dayEnd);

            if (dayEnergy != null)
            {
                dataPoints.Add(new PowerAndEnergyMetricsDataPoint
                {
                    Timestamp = day.ToString("yyyy-MM-dd"),

                    // Energy values in "Power" fields (kWh despite field names)
                    SolarPower = Math.Round(dayEnergy.PvTotalEnergyKWh ?? 0, 3),
                    LoadPower = Math.Round(dayEnergy.LoadEnergyKWh ?? 0, 3),
                    GeneratorPower = Math.Round(dayEnergy.GeneratorEnergyKWh ?? 0, 3),
                    GridPower = Math.Round((dayEnergy.GridImportEnergyKWh ?? 0) - (dayEnergy.GridExportEnergyKWh ?? 0), 3),
                    BatteryPower = Math.Round((dayEnergy.BatteryDischargeEnergyKWh ?? 0) - (dayEnergy.BatteryChargeEnergyKWh ?? 0), 3),

                    // Detailed energy breakdown
                    GridImportEnergyKWh = Math.Round(dayEnergy.GridImportEnergyKWh ?? 0, 3),
                    GridExportEnergyKWh = Math.Round(dayEnergy.GridExportEnergyKWh ?? 0, 3),
                    BatteryChargeEnergyKWh = Math.Round(dayEnergy.BatteryChargeEnergyKWh ?? 0, 3),
                    BatteryDischargeEnergyKWh = Math.Round(dayEnergy.BatteryDischargeEnergyKWh ?? 0, 3),
                    LoadEnergyKWh = Math.Round(dayEnergy.LoadEnergyKWh ?? 0, 3),
                    GeneratorEnergyKWh = Math.Round(dayEnergy.GeneratorEnergyKWh ?? 0, 3),

                    Unit = "kWh"
                });
            }
        }

        return CreateResponse("Month", dataPoints);
    }

    /// <summary>
    /// Yearly metrics: Aggregate monthly energy from DailyCumulativeEnergy samples
    /// Sums all daily energy values for each month
    /// Interpolates energy values for missing days before aggregation
    /// Uses device local timezone for accurate time-based queries
    /// </summary>
    private async Task<PowerAndEnergyMetricsResponseDto> GetYearlyMetricsAsync(Guid deviceId, Guid thingsBoardDeviceId, int year, CancellationToken cancellationToken)
    {
        // Calculate the start and end dates for the given year
        var yearStart = new DateTime(year, 1, 1);
        var yearStartLocal = DateTime.SpecifyKind(yearStart, DateTimeKind.Unspecified);
        var yearEnd = yearStart.AddYears(1);

        // Get all DailyCumulativeEnergy samples for the year
        // DeviceLocalTime is in device local time
        var dailyEnergyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DeviceLocalTime >= yearStartLocal &&
                       x.DeviceLocalTime < yearEnd &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .OrderBy(x => x.DeviceLocalTime)
            .ToListAsync(cancellationToken);

        // Apply energy interpolation for missing days
        var interpolatedEnergyData = InterpolateEnergyGaps(dailyEnergyData, TimeSpan.FromDays(1));

        var dataPoints = new List<PowerAndEnergyMetricsDataPoint>();

        // Process each month
        for (int month = 1; month <= 12; month++)
        {
            var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var monthEnd = monthStart.AddMonths(1);

            // Get all daily energy data for this month (after interpolation)
            var monthEnergyData = interpolatedEnergyData
                .Where(x => x.DeviceLocalTime >= monthStart && x.DeviceLocalTime < monthEnd)
                .ToList();

            if (monthEnergyData.Any())
            {
                // Sum all daily values to get monthly totals
                var monthlyPvEnergy = monthEnergyData.Sum(x => x.PvTotalEnergyKWh ?? 0);
                var monthlyGridImport = monthEnergyData.Sum(x => x.GridImportEnergyKWh ?? 0);
                var monthlyGridExport = monthEnergyData.Sum(x => x.GridExportEnergyKWh ?? 0);
                var monthlyBatteryCharge = monthEnergyData.Sum(x => x.BatteryChargeEnergyKWh ?? 0);
                var monthlyBatteryDischarge = monthEnergyData.Sum(x => x.BatteryDischargeEnergyKWh ?? 0);
                var monthlyLoad = monthEnergyData.Sum(x => x.LoadEnergyKWh ?? 0);
                var monthlyGenerator = monthEnergyData.Sum(x => x.GeneratorEnergyKWh ?? 0);

                dataPoints.Add(new PowerAndEnergyMetricsDataPoint
                {
                    Timestamp = new DateTime(year, month, 1).ToString("yyyy-MM"),

                    // Energy values in "Power" fields (kWh despite field names)
                    SolarPower = Math.Round(monthlyPvEnergy, 3),
                    LoadPower = Math.Round(monthlyLoad, 3),
                    GeneratorPower = Math.Round(monthlyGenerator, 3),
                    GridPower = Math.Round(monthlyGridImport - monthlyGridExport, 3),
                    BatteryPower = Math.Round(monthlyBatteryDischarge - monthlyBatteryCharge, 3),

                    // Detailed energy breakdown
                    GridImportEnergyKWh = Math.Round(monthlyGridImport, 3),
                    GridExportEnergyKWh = Math.Round(monthlyGridExport, 3),
                    BatteryChargeEnergyKWh = Math.Round(monthlyBatteryCharge, 3),
                    BatteryDischargeEnergyKWh = Math.Round(monthlyBatteryDischarge, 3),
                    LoadEnergyKWh = Math.Round(monthlyLoad, 3),
                    GeneratorEnergyKWh = Math.Round(monthlyGenerator, 3),

                    Unit = "kWh"
                });
            }
        }

        return CreateResponse("Year", dataPoints);
    }

    #region Helper Methods

    /// <summary>
    /// Interpolate missing power data using simple averaging
    /// When gaps exceed the expected sampling interval, fill with average of surrounding samples
    /// </summary>
    private List<DevicePowerData> InterpolatePowerGaps(List<DevicePowerData> data, TimeSpan expectedInterval)
    {
        if (data.Count < 2)
            return data;

        var interpolated = new List<DevicePowerData>();

        for (int i = 0; i < data.Count; i++)
        {
            interpolated.Add(data[i]);

            // Check if there's a gap to the next sample
            if (i < data.Count - 1)
            {
                var current = data[i];
                var next = data[i + 1];
                var gap = next.DeviceLocalTime - current.DeviceLocalTime;

                // If gap is more than 2x expected interval, interpolate
                if (gap > expectedInterval * 2)
                {
                    var missingIntervals = (int)(gap.TotalMinutes / expectedInterval.TotalMinutes) - 1;

                    // Create interpolated samples
                    for (int j = 1; j <= missingIntervals; j++)
                    {
                        var interpolatedSample = new DevicePowerData
                        {
                            ThingsBoardDeviceId = current.ThingsBoardDeviceId,
                            DeviceLocalTime = current.DeviceLocalTime.AddMinutes(expectedInterval.TotalMinutes * j),
                            DataType = current.DataType,
                            MessageType = current.MessageType,
                            PhaseType = current.PhaseType,

                            // Simple linear interpolation (average of current and next)
                            PvTotalPowerW = InterpolatePowerValue(current.PvTotalPowerW, next.PvTotalPowerW, missingIntervals + 1),
                            PvMppt1PowerW = InterpolatePowerValue(current.PvMppt1PowerW, next.PvMppt1PowerW, missingIntervals + 1),
                            PvMppt2PowerW = InterpolatePowerValue(current.PvMppt2PowerW, next.PvMppt2PowerW, missingIntervals + 1),
                            PvMppt3PowerW = InterpolatePowerValue(current.PvMppt3PowerW, next.PvMppt3PowerW, missingIntervals + 1),
                            PvMppt4PowerW = InterpolatePowerValue(current.PvMppt4PowerW, next.PvMppt4PowerW, missingIntervals + 1),

                            GridTotalPowerW = InterpolatePowerValue(current.GridTotalPowerW, next.GridTotalPowerW, missingIntervals + 1),
                            LoadTotalPowerW = InterpolatePowerValue(current.LoadTotalPowerW, next.LoadTotalPowerW, missingIntervals + 1),
                            BatteryPowerW = InterpolatePowerValue(current.BatteryPowerW, next.BatteryPowerW, missingIntervals + 1),
                            GeneratorTotalPowerW = InterpolatePowerValue(current.GeneratorTotalPowerW, next.GeneratorTotalPowerW, missingIntervals + 1)
                        };

                        interpolated.Add(interpolatedSample);
                    }
                }
            }
        }

        return interpolated;
    }

    /// <summary>
    /// Simple averaging interpolation for a single power value
    /// </summary>
    private double? InterpolatePowerValue(double? before, double? after, int intervals)
    {
        if (!before.HasValue || !after.HasValue)
            return null;

        return (before.Value + after.Value) / (intervals + 1);
    }

    /// <summary>
    /// Interpolate missing energy data using simple averaging
    /// When gaps exceed the expected sampling interval, fill with average of surrounding samples
    /// Used for DailyCumulativeEnergy data (kWh values)
    /// </summary>
    private List<DeviceEnergyData> InterpolateEnergyGaps(List<DeviceEnergyData> data, TimeSpan expectedInterval)
    {
        if (data.Count < 2)
            return data;

        var interpolated = new List<DeviceEnergyData>();

        for (int i = 0; i < data.Count; i++)
        {
            interpolated.Add(data[i]);

            // Check if there's a gap to the next sample
            if (i < data.Count - 1)
            {
                var current = data[i];
                var next = data[i + 1];
                var gap = next.DeviceLocalTime - current.DeviceLocalTime;

                // If gap is more than 2x expected interval, interpolate
                if (gap > expectedInterval * 2)
                {
                    var missingIntervals = (int)(gap.TotalDays / expectedInterval.TotalDays) - 1;

                    // Create interpolated samples
                    for (int j = 1; j <= missingIntervals; j++)
                    {
                        var interpolatedSample = new DeviceEnergyData
                        {
                            ThingsBoardDeviceId = current.ThingsBoardDeviceId,
                            DeviceLocalTime = current.DeviceLocalTime.AddDays(expectedInterval.TotalDays * j),
                            DataType = current.DataType,
                            MessageType = current.MessageType,
                            PhaseType = current.PhaseType,

                            // Simple linear interpolation (average of current and next)
                            PvTotalEnergyKWh = InterpolateEnergyValue(current.PvTotalEnergyKWh, next.PvTotalEnergyKWh, missingIntervals + 1),
                            PvMppt1EnergyKWh = InterpolateEnergyValue(current.PvMppt1EnergyKWh, next.PvMppt1EnergyKWh, missingIntervals + 1),
                            PvMppt2EnergyKWh = InterpolateEnergyValue(current.PvMppt2EnergyKWh, next.PvMppt2EnergyKWh, missingIntervals + 1),
                            PvMppt3EnergyKWh = InterpolateEnergyValue(current.PvMppt3EnergyKWh, next.PvMppt3EnergyKWh, missingIntervals + 1),
                            PvMppt4EnergyKWh = InterpolateEnergyValue(current.PvMppt4EnergyKWh, next.PvMppt4EnergyKWh, missingIntervals + 1),

                            GridImportEnergyKWh = InterpolateEnergyValue(current.GridImportEnergyKWh, next.GridImportEnergyKWh, missingIntervals + 1),
                            GridExportEnergyKWh = InterpolateEnergyValue(current.GridExportEnergyKWh, next.GridExportEnergyKWh, missingIntervals + 1),

                            BatteryChargeEnergyKWh = InterpolateEnergyValue(current.BatteryChargeEnergyKWh, next.BatteryChargeEnergyKWh, missingIntervals + 1),
                            BatteryDischargeEnergyKWh = InterpolateEnergyValue(current.BatteryDischargeEnergyKWh, next.BatteryDischargeEnergyKWh, missingIntervals + 1),

                            LoadEnergyKWh = InterpolateEnergyValue(current.LoadEnergyKWh, next.LoadEnergyKWh, missingIntervals + 1),
                            GeneratorEnergyKWh = InterpolateEnergyValue(current.GeneratorEnergyKWh, next.GeneratorEnergyKWh, missingIntervals + 1)
                        };

                        interpolated.Add(interpolatedSample);
                    }
                }
            }
        }

        return interpolated;
    }

    /// <summary>
    /// Simple averaging interpolation for a single energy value
    /// </summary>
    private double? InterpolateEnergyValue(double? before, double? after, int intervals)
    {
        if (!before.HasValue || !after.HasValue)
            return null;

        return (before.Value + after.Value) / (intervals + 1);
    }

    /// <summary>
    /// Create empty response when no data available
    /// </summary>
    private PowerAndEnergyMetricsResponseDto CreateEmptyResponse(string range)
    {
        return new PowerAndEnergyMetricsResponseDto
        {
            Range = range,
            Data = new List<Hems_Backend.Application.Common.Models.PowerAndEnergyMetricsDataPoint>()
        };
    }

    /// <summary>
    /// Create response with calculated statistics
    /// </summary>
    private PowerAndEnergyMetricsResponseDto CreateResponse(string range, List<Hems_Backend.Application.Common.Models.PowerAndEnergyMetricsDataPoint> dataPoints)
    {
        if (!dataPoints.Any())
            return CreateEmptyResponse(range);

        var allSolarPowers = dataPoints.Select(x => x.SolarPower).ToList();
        var allGridPowers = dataPoints.Select(x => x.GridPower).ToList();
        var allBatteryPowers = dataPoints.Select(x => x.BatteryPower).ToList();
        var allLoadPowers = dataPoints.Select(x => x.LoadPower).ToList();

        return new PowerAndEnergyMetricsResponseDto
        {
            Range = range,
            Data = dataPoints           
        };
    }

    #endregion

    #region Demo Simulation Methods

    // Demo device timezone (Istanbul/Turkey - UTC+3)
    private static readonly TimeZoneInfo _demoTimezone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

    /// <summary>
    /// Generate simulated daily power metrics (5-minute intervals)
    /// Uses IDemoSimulationDataService for consistent data with PowerFlow endpoint
    /// </summary>
    private PowerAndEnergyMetricsResponseDto GetSimulatedDailyMetrics(DateOnly date)
    {
        var dataPoints = new List<PowerAndEnergyMetricsDataPoint>();

        // Use device local time for "today" check (demo device is in Istanbul timezone)
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _demoTimezone);
        var isToday = date == DateOnly.FromDateTime(nowLocal);

        // Demo device ID - we use a fixed one for simulation since we don't have request context here
        var demoDeviceId = Guid.Empty;

        // Generate 5-minute samples for the day using the shared simulation service
        for (int hour = 0; hour < 24; hour++)
        {
            for (int minute = 0; minute < 60; minute += 5)
            {
                // If today, only generate up to current LOCAL time
                if (isToday && (hour > nowLocal.Hour || (hour == nowLocal.Hour && minute > nowLocal.Minute)))
                    break;

                // Create timestamp for this sample
                var timestamp = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0, DateTimeKind.Utc);

                // Get power flow from the shared simulation service (returns kW)
                var powerFlow = _demoSimulationService.GetPowerFlowAtTime(demoDeviceId, timestamp);

                // Convert from kW to W for daily metrics display
                dataPoints.Add(new PowerAndEnergyMetricsDataPoint
                {
                    Timestamp = $"{hour:D2}:{minute:D2}",
                    SolarPower = Math.Round(powerFlow.SolarPowerKw * 1000, 2),     // kW to W
                    GridPower = Math.Round(powerFlow.GridPowerKw * 1000, 2),       // kW to W
                    BatteryPower = Math.Round(powerFlow.BatteryPowerKw * 1000, 2), // kW to W
                    LoadPower = Math.Round(powerFlow.LoadPowerKw * 1000, 2),       // kW to W
                    GeneratorPower = Math.Round(powerFlow.GeneratorPowerKw * 1000, 2),
                    Unit = "W"
                });
            }
        }

        return CreateResponse("Day", dataPoints);
    }

    /// <summary>
    /// Generate simulated weekly energy metrics (daily totals)
    /// Uses device local timezone for "today" check
    /// </summary>
    private PowerAndEnergyMetricsResponseDto GetSimulatedWeeklyMetrics(int year, int week)
    {
        var weekStart = ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
        var dataPoints = new List<PowerAndEnergyMetricsDataPoint>();

        // Use device local time for comparison
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _demoTimezone);

        for (int i = 0; i < 7; i++)
        {
            var day = weekStart.AddDays(i);
            if (day.Date > nowLocal.Date) break; // Use local date comparison

            var energy = GetSimulatedDailyEnergy(day);

            dataPoints.Add(new PowerAndEnergyMetricsDataPoint
            {
                Timestamp = day.ToString("yyyy-MM-dd"),
                SolarPower = Math.Round(energy.PvEnergyKWh, 3),
                LoadPower = Math.Round(energy.LoadEnergyKWh, 3),
                GeneratorPower = 0,
                GridPower = Math.Round(energy.GridImportEnergyKWh - energy.GridExportEnergyKWh, 3),
                BatteryPower = Math.Round(energy.BatteryDischargeEnergyKWh - energy.BatteryChargeEnergyKWh, 3),
                GridImportEnergyKWh = Math.Round(energy.GridImportEnergyKWh, 3),
                GridExportEnergyKWh = Math.Round(energy.GridExportEnergyKWh, 3),
                BatteryChargeEnergyKWh = Math.Round(energy.BatteryChargeEnergyKWh, 3),
                BatteryDischargeEnergyKWh = Math.Round(energy.BatteryDischargeEnergyKWh, 3),
                LoadEnergyKWh = Math.Round(energy.LoadEnergyKWh, 3),
                GeneratorEnergyKWh = 0,
                Unit = "kWh"
            });
        }

        return CreateResponse("Week", dataPoints);
    }

    /// <summary>
    /// Generate simulated monthly energy metrics (daily totals)
    /// Uses device local timezone for "today" check
    /// </summary>
    private PowerAndEnergyMetricsResponseDto GetSimulatedMonthlyMetrics(int year, int month)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var dataPoints = new List<PowerAndEnergyMetricsDataPoint>();

        // Use device local time for comparison
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _demoTimezone);

        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(year, month, day);
            if (date.Date > nowLocal.Date) break; // Use local date comparison

            var energy = GetSimulatedDailyEnergy(date);

            dataPoints.Add(new PowerAndEnergyMetricsDataPoint
            {
                Timestamp = date.ToString("yyyy-MM-dd"),
                SolarPower = Math.Round(energy.PvEnergyKWh, 3),
                LoadPower = Math.Round(energy.LoadEnergyKWh, 3),
                GeneratorPower = 0,
                GridPower = Math.Round(energy.GridImportEnergyKWh - energy.GridExportEnergyKWh, 3),
                BatteryPower = Math.Round(energy.BatteryDischargeEnergyKWh - energy.BatteryChargeEnergyKWh, 3),
                GridImportEnergyKWh = Math.Round(energy.GridImportEnergyKWh, 3),
                GridExportEnergyKWh = Math.Round(energy.GridExportEnergyKWh, 3),
                BatteryChargeEnergyKWh = Math.Round(energy.BatteryChargeEnergyKWh, 3),
                BatteryDischargeEnergyKWh = Math.Round(energy.BatteryDischargeEnergyKWh, 3),
                LoadEnergyKWh = Math.Round(energy.LoadEnergyKWh, 3),
                GeneratorEnergyKWh = 0,
                Unit = "kWh"
            });
        }

        return CreateResponse("Month", dataPoints);
    }

    /// <summary>
    /// Generate simulated yearly energy metrics (monthly totals)
    /// Uses device local timezone for "today" check
    /// </summary>
    private PowerAndEnergyMetricsResponseDto GetSimulatedYearlyMetrics(int year)
    {
        var dataPoints = new List<PowerAndEnergyMetricsDataPoint>();

        // Use device local time for comparison
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _demoTimezone);

        for (int month = 1; month <= 12; month++)
        {
            if (year > nowLocal.Year || (year == nowLocal.Year && month > nowLocal.Month)) break;

            var daysInMonth = DateTime.DaysInMonth(year, month);
            var lastDay = (year == nowLocal.Year && month == nowLocal.Month) ? nowLocal.Day : daysInMonth;

            double totalPv = 0, totalLoad = 0, totalGridImport = 0, totalGridExport = 0;
            double totalBatteryCharge = 0, totalBatteryDischarge = 0;

            for (int day = 1; day <= lastDay; day++)
            {
                var date = new DateTime(year, month, day);
                var energy = GetSimulatedDailyEnergy(date);
                totalPv += energy.PvEnergyKWh;
                totalLoad += energy.LoadEnergyKWh;
                totalGridImport += energy.GridImportEnergyKWh;
                totalGridExport += energy.GridExportEnergyKWh;
                totalBatteryCharge += energy.BatteryChargeEnergyKWh;
                totalBatteryDischarge += energy.BatteryDischargeEnergyKWh;
            }

            dataPoints.Add(new PowerAndEnergyMetricsDataPoint
            {
                Timestamp = $"{year}-{month:D2}",
                SolarPower = Math.Round(totalPv, 3),
                LoadPower = Math.Round(totalLoad, 3),
                GeneratorPower = 0,
                GridPower = Math.Round(totalGridImport - totalGridExport, 3),
                BatteryPower = Math.Round(totalBatteryDischarge - totalBatteryCharge, 3),
                GridImportEnergyKWh = Math.Round(totalGridImport, 3),
                GridExportEnergyKWh = Math.Round(totalGridExport, 3),
                BatteryChargeEnergyKWh = Math.Round(totalBatteryCharge, 3),
                BatteryDischargeEnergyKWh = Math.Round(totalBatteryDischarge, 3),
                LoadEnergyKWh = Math.Round(totalLoad, 3),
                GeneratorEnergyKWh = 0,
                Unit = "kWh"
            });
        }

        return CreateResponse("Year", dataPoints);
    }

    /// <summary>
    /// Get simulated daily energy totals for a specific date
    /// Uses IDemoSimulationDataService for consistent data across all endpoints
    /// </summary>
    private DailyEnergyData GetSimulatedDailyEnergy(DateTime date)
    {
        return _demoSimulationService.GetDailyEnergy(date);
    }

    #endregion
}
