using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Dashboard.Queries.GetPowerMetrics;

public class GetPowerAndEnergyMetricsQueryHandler : IRequestHandler<GetPowerAndEnergyMetricsQuery, PowerAndEnergyMetricsResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly IDeviceMappingService _deviceMapping;

    public GetPowerAndEnergyMetricsQueryHandler(IApplicationDbContext context, ISender sender, IDeviceMappingService deviceMapping)
    {
        _context = context;
        _sender = sender;
        _deviceMapping = deviceMapping;
    }

    public async Task<PowerAndEnergyMetricsResponseDto> Handle(GetPowerAndEnergyMetricsQuery request, CancellationToken cancellationToken)
    {
        // Device access kontrolü (uses local DeviceId)
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

        return request.Period.ToLower() switch
        {
            "daily" => await GetDailyMetricsAsync(thingsBoardDeviceId.Value, cancellationToken),
            "weekly" => await GetWeeklyMetricsAsync(thingsBoardDeviceId.Value, cancellationToken),
            "monthly" => await GetMonthlyMetricsAsync(thingsBoardDeviceId.Value, cancellationToken),
            "yearly" => await GetYearlyMetricsAsync(thingsBoardDeviceId.Value, cancellationToken),
            _ => await GetDailyMetricsAsync(thingsBoardDeviceId.Value, cancellationToken)
        };
    }

    /// <summary>
    /// Daily metrics: Return power data from 5-minute samples
    /// Uses DevicePowerData table which stores instantaneous power readings
    /// Interpolates power values for missing samples
    /// </summary>
    private async Task<PowerAndEnergyMetricsResponseDto> GetDailyMetricsAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var startOfDay = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);
        var endOfDay = startOfDay.AddDays(1);

        // Get all power samples for today (5-min intervals) from DevicePowerData
        var powerData = await _context.DevicePowerData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DataDateTime >= startOfDay &&
                       x.DataDateTime < endOfDay &&
                       x.DataType == DataTypes.PowerFlow)
            .OrderBy(x => x.DataDateTime)
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
                Timestamp = sample.DataDateTime.ToString("HH:mm"),

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
    /// </summary>
    private async Task<PowerAndEnergyMetricsResponseDto> GetWeeklyMetricsAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var weekStartUtc = DateTime.SpecifyKind(weekStart, DateTimeKind.Utc);
        var weekEnd = weekStartUtc.AddDays(7);

        // Get all DailyCumulativeEnergy samples for the week (one sample per day)
        var dailyEnergyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DataDateTime >= weekStartUtc &&
                       x.DataDateTime < weekEnd &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .OrderBy(x => x.DataDateTime)
            .ToListAsync(cancellationToken);

        // Apply energy interpolation for missing days
        var interpolatedEnergyData = InterpolateEnergyGaps(dailyEnergyData, TimeSpan.FromDays(1));

        var dataPoints = new List<PowerAndEnergyMetricsDataPoint>();

        // Process each day of the week
        for (int i = 0; i < 7; i++)
        {
            var day = weekStart.AddDays(i);
            var dayStart = new DateTime(day.Year, day.Month, day.Day, 0, 0, 0, DateTimeKind.Utc);
            var dayEnd = dayStart.AddDays(1);

            // Find the daily cumulative energy for this day (after interpolation)
            var dayEnergy = interpolatedEnergyData.FirstOrDefault(x => x.DataDateTime >= dayStart && x.DataDateTime < dayEnd);

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
    /// </summary>
    private async Task<PowerAndEnergyMetricsResponseDto> GetMonthlyMetricsAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthStartUtc = DateTime.SpecifyKind(monthStart, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        // Get all DailyCumulativeEnergy samples for the month
        var dailyEnergyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DataDateTime >= monthStartUtc &&
                       x.DataDateTime < monthEnd &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .OrderBy(x => x.DataDateTime)
            .ToListAsync(cancellationToken);

        // Apply energy interpolation for missing days
        var interpolatedEnergyData = InterpolateEnergyGaps(dailyEnergyData, TimeSpan.FromDays(1));

        var dataPoints = new List<PowerAndEnergyMetricsDataPoint>();

        // Process each day of the month
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        for (int i = 1; i <= daysInMonth; i++)
        {
            var day = new DateTime(today.Year, today.Month, i);
            if (day > today) break; // Don't include future days

            var dayStart = new DateTime(day.Year, day.Month, day.Day, 0, 0, 0, DateTimeKind.Utc);
            var dayEnd = dayStart.AddDays(1);

            // Find the daily cumulative energy for this day (after interpolation)
            var dayEnergy = interpolatedEnergyData.FirstOrDefault(x => x.DataDateTime >= dayStart && x.DataDateTime < dayEnd);

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
    /// </summary>
    private async Task<PowerAndEnergyMetricsResponseDto> GetYearlyMetricsAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var yearStart = new DateTime(today.Year, 1, 1);
        var yearStartUtc = DateTime.SpecifyKind(yearStart, DateTimeKind.Utc);
        var yearEnd = yearStart.AddYears(1);

        // Get all DailyCumulativeEnergy samples for the year
        var dailyEnergyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DataDateTime >= yearStartUtc &&
                       x.DataDateTime < yearEnd &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .OrderBy(x => x.DataDateTime)
            .ToListAsync(cancellationToken);

        // Apply energy interpolation for missing days
        var interpolatedEnergyData = InterpolateEnergyGaps(dailyEnergyData, TimeSpan.FromDays(1));

        var dataPoints = new List<PowerAndEnergyMetricsDataPoint>();

        // Process each month
        for (int month = 1; month <= 12; month++)
        {
            if (month > today.Month && today.Year == yearStart.Year) break; // Don't include future months

            var monthStart = new DateTime(today.Year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);

            // Get all daily energy data for this month (after interpolation)
            var monthEnergyData = interpolatedEnergyData
                .Where(x => x.DataDateTime >= monthStart && x.DataDateTime < monthEnd)
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
                    Timestamp = new DateTime(today.Year, month, 1).ToString("yyyy-MM"),

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
                var gap = next.DataDateTime - current.DataDateTime;

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
                            DataDateTime = current.DataDateTime.AddMinutes(expectedInterval.TotalMinutes * j),
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
                var gap = next.DataDateTime - current.DataDateTime;

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
                            DataDateTime = current.DataDateTime.AddDays(expectedInterval.TotalDays * j),
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
}
