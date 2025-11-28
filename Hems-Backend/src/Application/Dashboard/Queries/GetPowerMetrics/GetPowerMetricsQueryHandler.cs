using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Dashboard.Queries.GetPowerMetrics;

public class GetPowerMetricsQueryHandler : IRequestHandler<GetPowerMetricsQuery, PowerMetricsResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;

    public GetPowerMetricsQueryHandler(IApplicationDbContext context, ISender sender)
    {
        _context = context;
        _sender = sender;
    }

    public async Task<PowerMetricsResponseDto> Handle(GetPowerMetricsQuery request, CancellationToken cancellationToken)
    {
        // Device access kontrolü
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Device access denied");
        }

        return request.Period.ToLower() switch
        {
            "daily" => await GetDailyPowerMetricsAsync(request.DeviceId, cancellationToken),
            "weekly" => await GetWeeklyPowerMetricsAsync(request.DeviceId, cancellationToken),
            "monthly" => await GetMonthlyPowerMetricsAsync(request.DeviceId, cancellationToken),
            "yearly" => await GetYearlyPowerMetricsAsync(request.DeviceId, cancellationToken),
            _ => await GetDailyPowerMetricsAsync(request.DeviceId, cancellationToken)
        };
    }

    /// <summary>
    /// Daily metrics: Calculate energy deltas from cumulative 5-minute samples
    /// Returns individual 5-minute samples with timestamp and delta energy
    /// Each delta represents energy produced/consumed in that 5-minute period
    /// </summary>
    private async Task<PowerMetricsResponseDto> GetDailyPowerMetricsAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var startOfDay = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);
        var endOfDay = startOfDay.AddDays(1);

        // Get all cumulative energy samples for today (5-min intervals)
        var energyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == deviceId && 
                       x.DataDateTime >= startOfDay &&
                       x.DataDateTime < endOfDay &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .OrderBy(x => x.DataDateTime)
            .ToListAsync(cancellationToken);

        if (!energyData.Any())
        {
            return CreateEmptyResponse("Day");
        }

        // Calculate deltas between consecutive samples
        var energyDeltas = CalculateEnergyDeltas(energyData);

        // Apply interpolation for missing data (if gaps > 5 minutes)
        energyDeltas = InterpolateGaps(energyDeltas, TimeSpan.FromMinutes(5));

        // Convert each delta to a data point with its timestamp
        var dataPoints = energyDeltas
            .Select(x => new Hems_Backend.Application.Common.Models.PowerMetricsDataPoint
            {
                Timestamp = x.DataDateTime.ToString("HH:mm"),
                SolarPower = Math.Round(x.PvTotalEnergyKWh ?? 0, 3),
                GridPower = Math.Round((x.GridImportEnergyKWh ?? 0) - (x.GridExportEnergyKWh ?? 0), 3),
                BatteryPower = Math.Round((x.BatteryChargeEnergyKWh ?? 0) - (x.BatteryDischargeEnergyKWh ?? 0), 3),
                LoadPower = Math.Round(x.LoadEnergyKWh ?? 0, 3),
                Unit = "kWh"
            })
            .ToList();

        return CreateResponse("Day", dataPoints);
    }

    /// <summary>
    /// Weekly metrics: Calculate daily energy deltas from lifetime energy samples
    /// Falls back to DailyCumulativeEnergy for current day if lifetime sample not available
    /// </summary>
    private async Task<PowerMetricsResponseDto> GetWeeklyPowerMetricsAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var weekStartUtc = DateTime.SpecifyKind(weekStart, DateTimeKind.Utc);

        // Get all lifetime energy samples for the week
        var lifetimeData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == deviceId && 
                       x.DataDateTime >= weekStartUtc &&
                       x.DataType == DataTypes.LifetimeEnergy)
            .OrderBy(x => x.DataDateTime)
            .ToListAsync(cancellationToken);

        // Calculate deltas between consecutive lifetime samples
        var energyDeltas = CalculateEnergyDeltas(lifetimeData);
        
        // Apply interpolation for missing days
        energyDeltas = InterpolateGaps(energyDeltas, TimeSpan.FromDays(1));

        var dataPoints = new List<Hems_Backend.Application.Common.Models.PowerMetricsDataPoint>();

        // Group by day
        for (int i = 0; i < 7; i++)
        {
            var day = weekStart.AddDays(i);
            var dayStart = new DateTime(day.Year, day.Month, day.Day, 0, 0, 0, DateTimeKind.Utc);
            var dayEnd = dayStart.AddDays(1);

            // Find delta for this day
            var dayDelta = energyDeltas.FirstOrDefault(x => x.DataDateTime >= dayStart && x.DataDateTime < dayEnd);

            // If current day and no lifetime sample exists, use DailyCumulativeEnergy
            if (dayDelta == null && day.Date == today)
            {
                dayDelta = await GetCurrentDayEnergyFromDailyData(deviceId, cancellationToken);
            }

            if (dayDelta != null)
            {
                dataPoints.Add(new Hems_Backend.Application.Common.Models.PowerMetricsDataPoint
                {
                    Timestamp = $"{day:MMM dd}",
                    SolarPower = Math.Round(dayDelta.PvTotalEnergyKWh ?? 0, 3),
                    GridPower = Math.Round((dayDelta.GridImportEnergyKWh ?? 0) - (dayDelta.GridExportEnergyKWh ?? 0), 3),
                    BatteryPower = Math.Round((dayDelta.BatteryChargeEnergyKWh ?? 0) - (dayDelta.BatteryDischargeEnergyKWh ?? 0), 3),
                    LoadPower = Math.Round(dayDelta.LoadEnergyKWh ?? 0, 3),
                    Unit = "kWh"
                });
            }
        }

        return CreateResponse("Week", dataPoints);
    }

    /// <summary>
    /// Monthly metrics: Calculate daily energy deltas from lifetime energy samples
    /// Falls back to DailyCumulativeEnergy for current day if lifetime sample not available
    /// </summary>
    private async Task<PowerMetricsResponseDto> GetMonthlyPowerMetricsAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthStartUtc = DateTime.SpecifyKind(monthStart, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        // Get all lifetime energy samples for the month
        var lifetimeData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == deviceId && 
                       x.DataDateTime >= monthStartUtc &&
                       x.DataDateTime < monthEnd &&
                       x.DataType == DataTypes.LifetimeEnergy)
            .OrderBy(x => x.DataDateTime)
            .ToListAsync(cancellationToken);

        // Calculate deltas between consecutive lifetime samples
        var energyDeltas = CalculateEnergyDeltas(lifetimeData);
        
        // Apply interpolation for missing days
        energyDeltas = InterpolateGaps(energyDeltas, TimeSpan.FromDays(1));

        var dataPoints = new List<Hems_Backend.Application.Common.Models.PowerMetricsDataPoint>();

        // Group by day
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        for (int i = 1; i <= daysInMonth; i++)
        {
            var day = new DateTime(today.Year, today.Month, i);
            if (day > today) break; // Don't include future days

            var dayStart = new DateTime(day.Year, day.Month, day.Day, 0, 0, 0, DateTimeKind.Utc);
            var dayEnd = dayStart.AddDays(1);

            // Find delta for this day
            var dayDelta = energyDeltas.FirstOrDefault(x => x.DataDateTime >= dayStart && x.DataDateTime < dayEnd);

            // If current day and no lifetime sample exists, use DailyCumulativeEnergy
            if (dayDelta == null && day.Date == today)
            {
                dayDelta = await GetCurrentDayEnergyFromDailyData(deviceId, cancellationToken);
            }

            if (dayDelta != null)
            {
                dataPoints.Add(new Hems_Backend.Application.Common.Models.PowerMetricsDataPoint
                {
                    Timestamp = $"Day {i}",
                    SolarPower = Math.Round(dayDelta.PvTotalEnergyKWh ?? 0, 3),
                    GridPower = Math.Round((dayDelta.GridImportEnergyKWh ?? 0) - (dayDelta.GridExportEnergyKWh ?? 0), 3),
                    BatteryPower = Math.Round((dayDelta.BatteryChargeEnergyKWh ?? 0) - (dayDelta.BatteryDischargeEnergyKWh ?? 0), 3),
                    LoadPower = Math.Round(dayDelta.LoadEnergyKWh ?? 0, 3),
                    Unit = "kWh"
                });
            }
        }

        return CreateResponse("Month", dataPoints);
    }

    /// <summary>
    /// Yearly metrics: Calculate monthly energy aggregates from lifetime energy samples
    /// Falls back to DailyCumulativeEnergy for current month if lifetime samples not available
    /// </summary>
    private async Task<PowerMetricsResponseDto> GetYearlyPowerMetricsAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var yearStart = new DateTime(today.Year, 1, 1);
        var yearStartUtc = DateTime.SpecifyKind(yearStart, DateTimeKind.Utc);
        var yearEnd = yearStart.AddYears(1);

        // Get all lifetime energy samples for the year
        var lifetimeData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == deviceId && 
                       x.DataDateTime >= yearStartUtc &&
                       x.DataDateTime < yearEnd &&
                       x.DataType == DataTypes.LifetimeEnergy)
            .OrderBy(x => x.DataDateTime)
            .ToListAsync(cancellationToken);

        // Calculate deltas between consecutive lifetime samples
        var energyDeltas = CalculateEnergyDeltas(lifetimeData);
        
        // Apply interpolation for missing days
        energyDeltas = InterpolateGaps(energyDeltas, TimeSpan.FromDays(1));

        var dataPoints = new List<Hems_Backend.Application.Common.Models.PowerMetricsDataPoint>();

        // Group by month
        for (int month = 1; month <= 12; month++)
        {
            if (month > today.Month && today.Year == yearStart.Year) break; // Don't include future months

            var monthStart = new DateTime(today.Year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);

            // Sum all daily deltas for this month
            var monthDeltas = energyDeltas.Where(x => x.DataDateTime >= monthStart && x.DataDateTime < monthEnd).ToList();

            // If current month and no sufficient lifetime samples, use DailyCumulativeEnergy
            if ((!monthDeltas.Any() || month == today.Month) && today.Month == month)
            {
                var currentMonthDelta = await GetCurrentMonthEnergyFromDailyData(deviceId, cancellationToken);
                if (currentMonthDelta != null)
                {
                    monthDeltas.Add(currentMonthDelta);
                }
            }

            if (monthDeltas.Any())
            {
                dataPoints.Add(new Hems_Backend.Application.Common.Models.PowerMetricsDataPoint
                {
                    Timestamp = new DateTime(today.Year, month, 1).ToString("MMM"),
                    SolarPower = Math.Round(monthDeltas.Sum(x => x.PvTotalEnergyKWh ?? 0), 3),
                    GridPower = Math.Round(monthDeltas.Sum(x => (x.GridImportEnergyKWh ?? 0) - (x.GridExportEnergyKWh ?? 0)), 3),
                    BatteryPower = Math.Round(monthDeltas.Sum(x => (x.BatteryChargeEnergyKWh ?? 0) - (x.BatteryDischargeEnergyKWh ?? 0)), 3),
                    LoadPower = Math.Round(monthDeltas.Sum(x => x.LoadEnergyKWh ?? 0), 3),
                    Unit = "kWh"
                });
            }
        }

        return CreateResponse("Year", dataPoints);
    }

    #region Helper Methods

    /// <summary>
    /// Calculate energy deltas between consecutive cumulative samples
    /// Each delta represents the energy produced/consumed between two samples
    /// </summary>
    private List<DeviceEnergyData> CalculateEnergyDeltas(List<DeviceEnergyData> cumulativeData)
    {
        if (cumulativeData.Count < 2)
            return cumulativeData;

        var deltas = new List<DeviceEnergyData>();

        for (int i = 1; i < cumulativeData.Count; i++)
        {
            var previous = cumulativeData[i - 1];
            var current = cumulativeData[i];

            // Create a new entity representing the delta
            var delta = new DeviceEnergyData
            {
                ThingsBoardDeviceId = current.ThingsBoardDeviceId,
                DataDateTime = current.DataDateTime,
                DataType = current.DataType,
                MessageType = current.MessageType,
                PhaseType = current.PhaseType,
                
                // Calculate deltas (current - previous)
                PvTotalEnergyKWh = CalculateDelta(current.PvTotalEnergyKWh, previous.PvTotalEnergyKWh),
                PvMppt1EnergyKWh = CalculateDelta(current.PvMppt1EnergyKWh, previous.PvMppt1EnergyKWh),
                PvMppt2EnergyKWh = CalculateDelta(current.PvMppt2EnergyKWh, previous.PvMppt2EnergyKWh),
                PvMppt3EnergyKWh = CalculateDelta(current.PvMppt3EnergyKWh, previous.PvMppt3EnergyKWh),
                PvMppt4EnergyKWh = CalculateDelta(current.PvMppt4EnergyKWh, previous.PvMppt4EnergyKWh),
                
                GridImportEnergyKWh = CalculateDelta(current.GridImportEnergyKWh, previous.GridImportEnergyKWh),
                GridExportEnergyKWh = CalculateDelta(current.GridExportEnergyKWh, previous.GridExportEnergyKWh),
                
                BatteryChargeEnergyKWh = CalculateDelta(current.BatteryChargeEnergyKWh, previous.BatteryChargeEnergyKWh),
                BatteryDischargeEnergyKWh = CalculateDelta(current.BatteryDischargeEnergyKWh, previous.BatteryDischargeEnergyKWh),
                
                LoadEnergyKWh = CalculateDelta(current.LoadEnergyKWh, previous.LoadEnergyKWh),
                GeneratorEnergyKWh = CalculateDelta(current.GeneratorEnergyKWh, previous.GeneratorEnergyKWh)
            };

            deltas.Add(delta);
        }

        return deltas;
    }

    /// <summary>
    /// Calculate delta with null handling - returns null if either value is null
    /// Also handles reset cases (when current < previous, assume counter reset)
    /// </summary>
    private double? CalculateDelta(double? current, double? previous)
    {
        if (!current.HasValue || !previous.HasValue)
            return null;

        var delta = current.Value - previous.Value;
        
        // Handle counter reset (e.g., daily cumulative resets at midnight)
        // If delta is negative and absolute value is large, assume it's the new value after reset
        if (delta < 0 && Math.Abs(delta) > current.Value)
            return current.Value;
        
        // If delta is negative but small, it might be actual negative energy (export), keep it
        return delta;
    }

    /// <summary>
    /// Interpolate missing data using simple averaging
    /// When gaps exceed the expected sampling interval, fill with average of surrounding samples
    /// </summary>
    private List<DeviceEnergyData> InterpolateGaps(List<DeviceEnergyData> data, TimeSpan expectedInterval)
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
                    var missingIntervals = (int)(gap.TotalMinutes / expectedInterval.TotalMinutes) - 1;
                    
                    // Create interpolated samples
                    for (int j = 1; j <= missingIntervals; j++)
                    {
                        var interpolatedSample = new DeviceEnergyData
                        {
                            ThingsBoardDeviceId = current.ThingsBoardDeviceId,
                            DataDateTime = current.DataDateTime.AddMinutes(expectedInterval.TotalMinutes * j),
                            DataType = current.DataType,
                            MessageType = current.MessageType,
                            PhaseType = current.PhaseType,
                            
                            // Simple linear interpolation (average of current and next)
                            PvTotalEnergyKWh = InterpolateValue(current.PvTotalEnergyKWh, next.PvTotalEnergyKWh, missingIntervals + 1),
                            PvMppt1EnergyKWh = InterpolateValue(current.PvMppt1EnergyKWh, next.PvMppt1EnergyKWh, missingIntervals + 1),
                            PvMppt2EnergyKWh = InterpolateValue(current.PvMppt2EnergyKWh, next.PvMppt2EnergyKWh, missingIntervals + 1),
                            PvMppt3EnergyKWh = InterpolateValue(current.PvMppt3EnergyKWh, next.PvMppt3EnergyKWh, missingIntervals + 1),
                            PvMppt4EnergyKWh = InterpolateValue(current.PvMppt4EnergyKWh, next.PvMppt4EnergyKWh, missingIntervals + 1),
                            
                            GridImportEnergyKWh = InterpolateValue(current.GridImportEnergyKWh, next.GridImportEnergyKWh, missingIntervals + 1),
                            GridExportEnergyKWh = InterpolateValue(current.GridExportEnergyKWh, next.GridExportEnergyKWh, missingIntervals + 1),
                            
                            BatteryChargeEnergyKWh = InterpolateValue(current.BatteryChargeEnergyKWh, next.BatteryChargeEnergyKWh, missingIntervals + 1),
                            BatteryDischargeEnergyKWh = InterpolateValue(current.BatteryDischargeEnergyKWh, next.BatteryDischargeEnergyKWh, missingIntervals + 1),
                            
                            LoadEnergyKWh = InterpolateValue(current.LoadEnergyKWh, next.LoadEnergyKWh, missingIntervals + 1),
                            GeneratorEnergyKWh = InterpolateValue(current.GeneratorEnergyKWh, next.GeneratorEnergyKWh, missingIntervals + 1)
                        };
                        
                        interpolated.Add(interpolatedSample);
                    }
                }
            }
        }

        return interpolated;
    }

    /// <summary>
    /// Simple averaging interpolation for a single value
    /// </summary>
    private double? InterpolateValue(double? before, double? after, int intervals)
    {
        if (!before.HasValue || !after.HasValue)
            return null;
        
        return (before.Value + after.Value) / (intervals + 1);
    }

    /// <summary>
    /// Get current day's total energy from DailyCumulativeEnergy
    /// Uses the last sample of the day (most recent cumulative value)
    /// </summary>
    private async Task<DeviceEnergyData?> GetCurrentDayEnergyFromDailyData(Guid deviceId, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var startOfDay = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);
        var endOfDay = startOfDay.AddDays(1);

        // Get the most recent cumulative sample (represents total energy from midnight to now)
        return await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == deviceId && 
                       x.DataDateTime >= startOfDay &&
                       x.DataDateTime < endOfDay &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .OrderByDescending(x => x.DataDateTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Get current month's total energy by summing all deltas from DailyCumulativeEnergy
    /// </summary>
    private async Task<DeviceEnergyData?> GetCurrentMonthEnergyFromDailyData(Guid deviceId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var dailyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == deviceId && 
                       x.DataDateTime >= monthStart &&
                       x.DataDateTime < monthEnd &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .OrderBy(x => x.DataDateTime)
            .ToListAsync(cancellationToken);

        if (!dailyData.Any())
            return null;

        // Group by day and get the last sample of each day (cumulative for that day)
        var dailyTotals = dailyData
            .GroupBy(x => x.DataDateTime.Date)
            .Select(g => g.OrderByDescending(x => x.DataDateTime).First())
            .ToList();

        // Sum all daily totals
        return new DeviceEnergyData
        {
            ThingsBoardDeviceId = deviceId,
            DataDateTime = today,
            DataType = DataTypes.DailyCumulativeEnergy,
            
            PvTotalEnergyKWh = dailyTotals.Sum(x => x.PvTotalEnergyKWh ?? 0),
            PvMppt1EnergyKWh = dailyTotals.Sum(x => x.PvMppt1EnergyKWh ?? 0),
            PvMppt2EnergyKWh = dailyTotals.Sum(x => x.PvMppt2EnergyKWh ?? 0),
            PvMppt3EnergyKWh = dailyTotals.Sum(x => x.PvMppt3EnergyKWh ?? 0),
            PvMppt4EnergyKWh = dailyTotals.Sum(x => x.PvMppt4EnergyKWh ?? 0),
            
            GridImportEnergyKWh = dailyTotals.Sum(x => x.GridImportEnergyKWh ?? 0),
            GridExportEnergyKWh = dailyTotals.Sum(x => x.GridExportEnergyKWh ?? 0),
            
            BatteryChargeEnergyKWh = dailyTotals.Sum(x => x.BatteryChargeEnergyKWh ?? 0),
            BatteryDischargeEnergyKWh = dailyTotals.Sum(x => x.BatteryDischargeEnergyKWh ?? 0),
            
            LoadEnergyKWh = dailyTotals.Sum(x => x.LoadEnergyKWh ?? 0),
            GeneratorEnergyKWh = dailyTotals.Sum(x => x.GeneratorEnergyKWh ?? 0)
        };
    }

    /// <summary>
    /// Create empty response when no data available
    /// </summary>
    private PowerMetricsResponseDto CreateEmptyResponse(string range)
    {
        return new PowerMetricsResponseDto
        {
            Range = range,
            Data = new List<Hems_Backend.Application.Common.Models.PowerMetricsDataPoint>()
        };
    }

    /// <summary>
    /// Create response with calculated statistics
    /// </summary>
    private PowerMetricsResponseDto CreateResponse(string range, List<Hems_Backend.Application.Common.Models.PowerMetricsDataPoint> dataPoints)
    {
        if (!dataPoints.Any())
            return CreateEmptyResponse(range);

        var allSolarPowers = dataPoints.Select(x => x.SolarPower).ToList();
        var allGridPowers = dataPoints.Select(x => x.GridPower).ToList();
        var allBatteryPowers = dataPoints.Select(x => x.BatteryPower).ToList();
        var allLoadPowers = dataPoints.Select(x => x.LoadPower).ToList();

        return new PowerMetricsResponseDto
        {
            Range = range,
            Data = dataPoints           
        };
    }

    #endregion
}
