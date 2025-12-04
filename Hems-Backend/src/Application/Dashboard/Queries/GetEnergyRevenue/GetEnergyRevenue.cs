using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Hems_Backend.Application.Dashboard.Queries.GetEnergyRevenue;

public record GetEnergyRevenueQuery : IRequest<EnergyRevenueDto>
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

public class GetEnergyRevenueQueryHandler : IRequestHandler<GetEnergyRevenueQuery, EnergyRevenueDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly IDeviceMappingService _deviceMapping;

    // Netherlands energy market prices (EUR per kWh)
    private const double BuyPricePerKWh = 0.35;  // Retail electricity price
    private const double SellPricePerKWh = 0.10; // Feed-in tariff

    public GetEnergyRevenueQueryHandler(
        IApplicationDbContext context,
        ISender sender,
        IDeviceMappingService deviceMapping)
    {
        _context = context;
        _sender = sender;
        _deviceMapping = deviceMapping;
    }

    public async Task<EnergyRevenueDto> Handle(GetEnergyRevenueQuery request, CancellationToken cancellationToken)
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

        var period = request.Period.ToLower();
        List<EnergyRevenueDataPoint> dataPoints;

        switch (period)
        {
            case "day":
                if (!request.Date.HasValue)
                    throw new ArgumentException("Date is required for period 'day'");
                dataPoints = await GetDayDataAsync(thingsBoardDeviceId.Value, request.Date.Value, cancellationToken);
                break;
            case "week":
                if (!request.Year.HasValue || !request.Week.HasValue)
                    throw new ArgumentException("Year and Week are required for period 'week'");
                dataPoints = await GetWeekDataAsync(thingsBoardDeviceId.Value, request.Year.Value, request.Week.Value, cancellationToken);
                break;
            case "month":
                if (!request.Year.HasValue || !request.Month.HasValue)
                    throw new ArgumentException("Year and Month are required for period 'month'");
                dataPoints = await GetMonthDataAsync(thingsBoardDeviceId.Value, request.Year.Value, request.Month.Value, cancellationToken);
                break;
            case "year":
                if (!request.Year.HasValue)
                    throw new ArgumentException("Year is required for period 'year'");
                dataPoints = await GetYearDataAsync(thingsBoardDeviceId.Value, request.Year.Value, cancellationToken);
                break;
            default:
                throw new ArgumentException($"Invalid period: {period}. Must be one of: day, week, month, year");
        }

        // Calculate totals in EUR
        var totalBuyKWh = dataPoints.Sum(d => d.Buy);
        var totalSellKWh = dataPoints.Sum(d => d.Sell);
        var totalBuyPrice = totalBuyKWh * BuyPricePerKWh;
        var totalSellPrice = totalSellKWh * SellPricePerKWh;
        var totalNetPrice = totalSellPrice - totalBuyPrice;

        return new EnergyRevenueDto
        {
            Period = period,
            BuyPricePerKWh = BuyPricePerKWh,
            SellPricePerKWh = SellPricePerKWh,
            Data = dataPoints,
            Total = new EnergyRevenueTotal
            {
                BuyPrice = Math.Round(totalBuyPrice, 2),
                SellPrice = Math.Round(totalSellPrice, 2),
                NetPrice = Math.Round(totalNetPrice, 2)
            }
        };
    }

    /// <summary>
    /// Get hourly revenue data for a single day (24 data points)
    /// Calculates revenue from PowerFlow data (5-minute intervals)
    /// </summary>
    private async Task<List<EnergyRevenueDataPoint>> GetDayDataAsync(Guid thingsBoardDeviceId, DateOnly date, CancellationToken cancellationToken)
    {
        var dayStart = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        // Get all PowerFlow data for the day (5-minute intervals)
        var powerFlowData = await _context.DevicePowerData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DataDateTime >= dayStart &&
                       x.DataDateTime < dayEnd &&
                       x.DataType == DataTypes.PowerFlow)
            .OrderBy(x => x.DataDateTime)
            .ToListAsync(cancellationToken);

        // Apply interpolation for missing data (if gaps > 5 minutes)
        var interpolatedData = InterpolatePowerFlowGaps(powerFlowData, TimeSpan.FromMinutes(5));

        var result = new List<EnergyRevenueDataPoint>();

        // Generate 24 hourly data points
        for (int hour = 0; hour < 24; hour++)
        {
            var hourStart = dayStart.AddHours(hour);
            var hourEnd = hourStart.AddHours(1);

            // Get all power samples for this hour
            var hourSamples = interpolatedData
                .Where(x => x.DataDateTime >= hourStart && x.DataDateTime < hourEnd)
                .ToList();

            double buyEnergyKWh = 0;
            double sellEnergyKWh = 0;

            if (hourSamples.Any())
            {
                // Calculate energy from power samples
                // Energy (kWh) = Power (W) * Time (hours) / 1000
                // For 5-minute intervals: kWh = W * (5/60) / 1000 = W * 0.0833... / 1000
                const double timeIntervalHours = 5.0 / 60.0; // 5 minutes = 0.0833... hours

                foreach (var sample in hourSamples)
                {
                    var gridPowerW = sample.GridTotalPowerW ?? 0;

                    if (gridPowerW > 0)
                    {
                        // Positive power = importing from grid (buying)
                        buyEnergyKWh += (gridPowerW * timeIntervalHours) / 1000.0;
                    }
                    else if (gridPowerW < 0)
                    {
                        // Negative power = exporting to grid (selling)
                        sellEnergyKWh += (Math.Abs(gridPowerW) * timeIntervalHours) / 1000.0;
                    }
                }
            }

            // Calculate revenue for this hour
            var net = sellEnergyKWh - buyEnergyKWh;
            var price = (sellEnergyKWh * SellPricePerKWh) - (buyEnergyKWh * BuyPricePerKWh);

            result.Add(new EnergyRevenueDataPoint
            {
                Timestamp = hourStart.ToString("HH:mm"),
                Buy = Math.Round(buyEnergyKWh, 3),
                Sell = Math.Round(sellEnergyKWh, 3),
                Net = Math.Round(net, 3),
                Price = Math.Round(price, 2)
            });
        }

        return result;
    }

    /// <summary>
    /// Interpolate missing power flow data using simple averaging
    /// When gaps exceed the expected sampling interval, fill with average of surrounding samples
    /// </summary>
    private List<Domain.Entities.DevicePowerData> InterpolatePowerFlowGaps(
        List<Domain.Entities.DevicePowerData> data,
        TimeSpan expectedInterval)
    {
        if (data.Count < 2)
            return data;

        var interpolated = new List<Domain.Entities.DevicePowerData>();

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
                        var interpolatedSample = new Domain.Entities.DevicePowerData
                        {
                            ThingsBoardDeviceId = current.ThingsBoardDeviceId,
                            DataDateTime = current.DataDateTime.AddMinutes(expectedInterval.TotalMinutes * j),
                            DataType = current.DataType,
                            MessageType = current.MessageType,
                            PhaseType = current.PhaseType,

                            // Simple linear interpolation (average of current and next)
                            GridTotalPowerW = InterpolatePowerValue(current.GridTotalPowerW, next.GridTotalPowerW),
                            PvTotalPowerW = InterpolatePowerValue(current.PvTotalPowerW, next.PvTotalPowerW),
                            LoadTotalPowerW = InterpolatePowerValue(current.LoadTotalPowerW, next.LoadTotalPowerW),
                            BatteryPowerW = InterpolatePowerValue(current.BatteryPowerW, next.BatteryPowerW),
                            GeneratorTotalPowerW = InterpolatePowerValue(current.GeneratorTotalPowerW, next.GeneratorTotalPowerW)
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
    private double? InterpolatePowerValue(double? before, double? after)
    {
        if (!before.HasValue || !after.HasValue)
            return before ?? after;

        return (before.Value + after.Value) / 2.0;
    }

    /// <summary>
    /// Get daily revenue data for a specific week (7 days)
    /// </summary>
    private async Task<List<EnergyRevenueDataPoint>> GetWeekDataAsync(Guid thingsBoardDeviceId, int year, int week, CancellationToken cancellationToken)
    {
        // Calculate the start and end dates for the given ISO week
        var weekStart = ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
        var weekStartUtc = DateTime.SpecifyKind(weekStart, DateTimeKind.Utc);
        var weekEnd = weekStartUtc.AddDays(7);

        var dailyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DataDateTime >= weekStartUtc &&
                       x.DataDateTime < weekEnd &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .OrderBy(x => x.DataDateTime)
            .ToListAsync(cancellationToken);

        // Group by date and take the most recent entry for each day
        var latestDailyData = dailyData
            .GroupBy(d => d.DataDateTime.Date)
            .Select(g => g.OrderByDescending(d => d.DataDateTime).First())
            .ToDictionary(d => d.DataDateTime.Date);

        // Generate all 7 days in the week
        var result = new List<EnergyRevenueDataPoint>();
        for (int i = 0; i < 7; i++)
        {
            var currentDate = weekStartUtc.AddDays(i);

            if (latestDailyData.TryGetValue(currentDate.Date, out var data))
            {
                var buy = data.GridImportEnergyKWh ?? 0;
                var sell = data.GridExportEnergyKWh ?? 0;
                var net = sell - buy;
                var price = (sell * SellPricePerKWh) - (buy * BuyPricePerKWh);

                result.Add(new EnergyRevenueDataPoint
                {
                    Timestamp = currentDate.ToString("yyyy-MM-dd"),
                    Buy = Math.Round(buy, 2),
                    Sell = Math.Round(sell, 2),
                    Net = Math.Round(net, 2),
                    Price = Math.Round(price, 2)
                });
            }
            else
            {
                // No data for this day, add zeros
                result.Add(new EnergyRevenueDataPoint
                {
                    Timestamp = currentDate.ToString("yyyy-MM-dd"),
                    Buy = 0,
                    Sell = 0,
                    Net = 0,
                    Price = 0
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Get daily revenue data for a specific month
    /// </summary>
    private async Task<List<EnergyRevenueDataPoint>> GetMonthDataAsync(Guid thingsBoardDeviceId, int year, int month, CancellationToken cancellationToken)
    {
        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var dailyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DataDateTime >= monthStart &&
                       x.DataDateTime < monthEnd &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .OrderBy(x => x.DataDateTime)
            .ToListAsync(cancellationToken);

        // Group by date and take the most recent entry for each day
        var latestDailyData = dailyData
            .GroupBy(d => d.DataDateTime.Date)
            .Select(g => g.OrderByDescending(d => d.DataDateTime).First())
            .ToDictionary(d => d.DataDateTime.Date);

        // Generate all days in the month
        var result = new List<EnergyRevenueDataPoint>();
        var currentDate = monthStart;
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var lastDate = monthStart.AddDays(daysInMonth - 1);

        while (currentDate <= lastDate)
        {
            if (latestDailyData.TryGetValue(currentDate, out var data))
            {
                var buy = data.GridImportEnergyKWh ?? 0;
                var sell = data.GridExportEnergyKWh ?? 0;
                var net = sell - buy;
                var price = (sell * SellPricePerKWh) - (buy * BuyPricePerKWh);

                result.Add(new EnergyRevenueDataPoint
                {
                    Timestamp = currentDate.ToString("yyyy-MM-dd"),
                    Buy = Math.Round(buy, 2),
                    Sell = Math.Round(sell, 2),
                    Net = Math.Round(net, 2),
                    Price = Math.Round(price, 2)
                });
            }
            else
            {
                // No data for this day, add zeros
                result.Add(new EnergyRevenueDataPoint
                {
                    Timestamp = currentDate.ToString("yyyy-MM-dd"),
                    Buy = 0,
                    Sell = 0,
                    Net = 0,
                    Price = 0
                });
            }

            currentDate = currentDate.AddDays(1);
        }

        return result;
    }

    /// <summary>
    /// Get monthly aggregated revenue data for a specific year
    /// </summary>
    private async Task<List<EnergyRevenueDataPoint>> GetYearDataAsync(Guid thingsBoardDeviceId, int year, CancellationToken cancellationToken)
    {
        var yearStart = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearEnd = yearStart.AddYears(1);

        var dailyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DataDateTime >= yearStart &&
                       x.DataDateTime < yearEnd &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .OrderBy(x => x.DataDateTime)
            .ToListAsync(cancellationToken);

        // Group by date and take the most recent entry for each day
        var latestDailyData = dailyData
            .GroupBy(d => d.DataDateTime.Date)
            .Select(g => g.OrderByDescending(d => d.DataDateTime).First());

        // Group by month and sum the values
        var monthlyData = latestDailyData
            .GroupBy(d => new { d.DataDateTime.Year, d.DataDateTime.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Buy = g.Sum(d => d.GridImportEnergyKWh ?? 0),
                Sell = g.Sum(d => d.GridExportEnergyKWh ?? 0)
            })
            .ToDictionary(x => x.Month);

        // Generate all 12 months of the specified year
        var result = new List<EnergyRevenueDataPoint>();

        for (int currentMonth = 1; currentMonth <= 12; currentMonth++)
        {
            if (monthlyData.TryGetValue(currentMonth, out var data))
            {
                var buy = data.Buy;
                var sell = data.Sell;
                var net = sell - buy;
                var price = (sell * SellPricePerKWh) - (buy * BuyPricePerKWh);

                result.Add(new EnergyRevenueDataPoint
                {
                    Timestamp = $"{year}-{currentMonth:D2}",
                    Buy = Math.Round(buy, 2),
                    Sell = Math.Round(sell, 2),
                    Net = Math.Round(net, 2),
                    Price = Math.Round(price, 2)
                });
            }
            else
            {
                // No data for this month, add zeros
                result.Add(new EnergyRevenueDataPoint
                {
                    Timestamp = $"{year}-{currentMonth:D2}",
                    Buy = 0,
                    Sell = 0,
                    Net = 0,
                    Price = 0
                });
            }
        }

        return result;
    }
}
