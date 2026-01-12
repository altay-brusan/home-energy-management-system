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

    public GetPowerAndEnergyMetricsQueryHandler(
        IApplicationDbContext context,
        ISender sender,
        IDeviceMappingService deviceMapping,
        ITimezoneService timezoneService,
        IDemoDataService demoDataService)
    {
        _context = context;
        _sender = sender;
        _deviceMapping = deviceMapping;
        _timezoneService = timezoneService;
        _demoDataService = demoDataService;
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
    /// Uses device local timezone for "today" check, not UTC
    /// </summary>
    private PowerAndEnergyMetricsResponseDto GetSimulatedDailyMetrics(DateOnly date)
    {
        var dataPoints = new List<PowerAndEnergyMetricsDataPoint>();

        // Use device local time for "today" check (demo device is in Istanbul timezone)
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _demoTimezone);
        var isToday = date == DateOnly.FromDateTime(nowLocal);

        // For smooth data, we need to track previous values
        double prevPvPower = 0, prevLoadPower = 0, prevGridPower = 0, prevBatteryPower = 0;

        // Generate 5-minute samples for the day
        for (int hour = 0; hour < 24; hour++)
        {
            for (int minute = 0; minute < 60; minute += 5)
            {
                // If today, only generate up to current LOCAL time
                if (isToday && (hour > nowLocal.Hour || (hour == nowLocal.Hour && minute > nowLocal.Minute)))
                    break;

                var timeOfDay = hour + (minute / 60.0);
                var (pvPower, loadPower, gridPower, batteryPower) = CalculateSimulatedPowerSmooth(
                    timeOfDay, date, prevPvPower, prevLoadPower, prevGridPower, prevBatteryPower);

                // Update previous values for smoothing
                prevPvPower = pvPower;
                prevLoadPower = loadPower;
                prevGridPower = gridPower;
                prevBatteryPower = batteryPower;

                dataPoints.Add(new PowerAndEnergyMetricsDataPoint
                {
                    Timestamp = $"{hour:D2}:{minute:D2}",
                    SolarPower = Math.Round(pvPower, 2),
                    GridPower = Math.Round(gridPower, 2),
                    BatteryPower = Math.Round(batteryPower, 2),
                    LoadPower = Math.Round(loadPower, 2),
                    GeneratorPower = 0,
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
    /// Calculate simulated power values with smooth transitions between samples.
    /// Uses sine waves and gradual changes for realistic-looking graphs.
    /// </summary>
    private static (double pvPower, double loadPower, double gridPower, double batteryPower) CalculateSimulatedPowerSmooth(
        double timeOfDay, DateOnly date, double prevPvPower, double prevLoadPower, double prevGridPower, double prevBatteryPower)
    {
        // Use date as seed for consistent day-to-day variation
        var daySeed = date.Year * 1000 + date.DayOfYear;

        // === PV Power: Smooth bell curve with subtle cloud variations ===
        double pvPower = 0;
        if (timeOfDay >= 5.5 && timeOfDay <= 20.5)
        {
            var solarNoon = 12.5; // Solar noon
            var hourFromNoon = timeOfDay - solarNoon;
            var maxPower = 4800.0; // 4.8kW peak
            var sigma = 3.8; // Spread of the curve

            // Smooth Gaussian curve for PV
            pvPower = maxPower * Math.Exp(-(hourFromNoon * hourFromNoon) / (2 * sigma * sigma));

            // Seasonal multiplier
            var seasonalMult = GetSeasonalPvMultiplier(date.Month);
            pvPower *= seasonalMult;

            // Add smooth cloud variation using sine waves (not random jumps)
            // Use multiple sine waves at different frequencies for natural-looking variation
            var cloudWave1 = Math.Sin(timeOfDay * 0.5 + daySeed * 0.1) * 0.03;
            var cloudWave2 = Math.Sin(timeOfDay * 1.2 + daySeed * 0.2) * 0.02;
            var cloudWave3 = Math.Sin(timeOfDay * 2.5 + daySeed * 0.3) * 0.01;
            var cloudVariation = 1.0 + cloudWave1 + cloudWave2 + cloudWave3;
            pvPower *= cloudVariation;

            // Ensure smooth sunrise/sunset transitions
            if (timeOfDay < 6.5)
            {
                var sunriseFactor = (timeOfDay - 5.5) / 1.0; // 0 to 1 over 1 hour
                pvPower *= Math.Sin(sunriseFactor * Math.PI / 2); // Smooth ease-in
            }
            else if (timeOfDay > 19.5)
            {
                var sunsetFactor = (20.5 - timeOfDay) / 1.0; // 1 to 0 over 1 hour
                pvPower *= Math.Sin(sunsetFactor * Math.PI / 2); // Smooth ease-out
            }
        }

        // === Load Power: Smooth transitions with sine-based patterns ===
        // Base load pattern using smooth sine waves
        var baseLoad = 500.0; // Minimum base load

        // Morning peak (7-9): smooth rise and fall
        var morningPeak = 800 * Math.Exp(-Math.Pow(timeOfDay - 8.0, 2) / 2.0);

        // Lunch bump (12-14): smaller peak
        var lunchBump = 400 * Math.Exp(-Math.Pow(timeOfDay - 13.0, 2) / 1.5);

        // Evening peak (18-21): largest peak
        var eveningPeak = 1800 * Math.Exp(-Math.Pow(timeOfDay - 19.5, 2) / 3.0);

        // Night reduction (0-5): lower base
        var nightReduction = (timeOfDay < 5 || timeOfDay > 23) ? -200 : 0;

        // Combine all components
        var targetLoad = baseLoad + morningPeak + lunchBump + eveningPeak + nightReduction;

        // Add subtle variation using sine waves (not random)
        var loadWave1 = Math.Sin(timeOfDay * 0.8 + daySeed * 0.15) * 50;
        var loadWave2 = Math.Sin(timeOfDay * 2.0 + daySeed * 0.25) * 30;
        targetLoad += loadWave1 + loadWave2;

        // Smooth transition from previous value (exponential moving average)
        double loadPower;
        if (prevLoadPower > 0)
        {
            var smoothingFactor = 0.3; // 0 = no change, 1 = instant change
            loadPower = prevLoadPower + (targetLoad - prevLoadPower) * smoothingFactor;
        }
        else
        {
            loadPower = targetLoad;
        }
        loadPower = Math.Max(200, loadPower); // Minimum load

        // === Energy Balance: Grid and Battery ===
        var netPower = pvPower - loadPower;
        double gridPower, batteryPower;

        if (netPower >= 0)
        {
            // Excess PV: charge battery first, then export to grid
            var batteryCharge = Math.Min(netPower * 0.65, 2800); // Max 2.8kW charge rate
            batteryPower = -batteryCharge;
            gridPower = -(netPower - batteryCharge); // Negative = export
        }
        else
        {
            // Deficit: discharge battery first, then import from grid
            var deficit = Math.Abs(netPower);
            var batteryDischarge = Math.Min(deficit * 0.55, 2400); // Max 2.4kW discharge rate
            batteryPower = batteryDischarge;
            gridPower = deficit - batteryDischarge; // Positive = import
        }

        // Smooth grid and battery transitions
        if (prevGridPower != 0 || prevBatteryPower != 0)
        {
            var gridSmoothing = 0.4;
            var batterySmoothing = 0.35;
            gridPower = prevGridPower + (gridPower - prevGridPower) * gridSmoothing;
            batteryPower = prevBatteryPower + (batteryPower - prevBatteryPower) * batterySmoothing;
        }

        return (Math.Max(0, pvPower), loadPower, gridPower, batteryPower);
    }

    /// <summary>
    /// Get simulated daily energy totals for a specific date
    /// </summary>
    private static SimulatedDailyEnergy GetSimulatedDailyEnergy(DateTime date)
    {
        var daySeed = date.Year * 1000 + date.DayOfYear;
        var dayRandom = new Random(daySeed);

        // Get seasonal and day type multipliers
        var pvMult = GetSeasonalPvMultiplier(date.Month);
        var loadMult = GetSeasonalLoadMultiplier(date.Month);

        // Day type (sunny, cloudy, etc.) based on random seed
        var dayTypeRand = dayRandom.NextDouble();
        var (pvTypeMult, importMult, exportMult) = dayTypeRand switch
        {
            < 0.30 => (1.0, 0.3, 1.0),    // sunny
            < 0.65 => (0.7, 0.6, 0.6),    // partly cloudy
            < 0.90 => (0.35, 1.0, 0.2),   // cloudy
            _ => (0.15, 1.5, 0.05)         // rainy
        };

        // Base values for a 5kW system on a sunny day
        var basePv = 32.5;
        var baseLoad = 18.0;
        var baseGridImport = 2.5;
        var baseGridExport = 12.0;
        var baseBatteryCharge = 8.0;
        var baseBatteryDischarge = 6.0;

        // Calculate final values with variation
        var variation = 0.9 + dayRandom.NextDouble() * 0.2;

        return new SimulatedDailyEnergy
        {
            PvEnergyKWh = basePv * pvMult * pvTypeMult * variation,
            LoadEnergyKWh = baseLoad * loadMult * variation,
            GridImportEnergyKWh = baseGridImport * importMult * (0.8 + dayRandom.NextDouble() * 0.4),
            GridExportEnergyKWh = baseGridExport * exportMult * (0.8 + dayRandom.NextDouble() * 0.4),
            BatteryChargeEnergyKWh = baseBatteryCharge * pvTypeMult * (0.8 + dayRandom.NextDouble() * 0.4),
            BatteryDischargeEnergyKWh = baseBatteryDischarge * (0.8 + dayRandom.NextDouble() * 0.4)
        };
    }

    private static double GetSeasonalPvMultiplier(int month) => month switch
    {
        1 => 0.25, 2 => 0.35, 3 => 0.55, 4 => 0.80, 5 => 1.00, 6 => 1.15,
        7 => 1.10, 8 => 1.00, 9 => 0.75, 10 => 0.50, 11 => 0.30, 12 => 0.20,
        _ => 1.0
    };

    private static double GetSeasonalLoadMultiplier(int month) => month switch
    {
        1 => 1.30, 2 => 1.25, 3 => 1.10, 4 => 0.95, 5 => 0.85, 6 => 0.80,
        7 => 0.82, 8 => 0.85, 9 => 0.95, 10 => 1.10, 11 => 1.25, 12 => 1.35,
        _ => 1.0
    };

    private record SimulatedDailyEnergy
    {
        public double PvEnergyKWh { get; init; }
        public double LoadEnergyKWh { get; init; }
        public double GridImportEnergyKWh { get; init; }
        public double GridExportEnergyKWh { get; init; }
        public double BatteryChargeEnergyKWh { get; init; }
        public double BatteryDischargeEnergyKWh { get; init; }
    }

    #endregion
}
