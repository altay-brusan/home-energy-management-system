using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Domain.Entities;
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

    // All prices for the device (loaded once per request)
    private List<DevicePrice> _allPrices = new();
    private string _currency = "EUR";

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

        // Load ALL prices for this device (for time-based calculations)
        _allPrices = await _context.DevicePrices
            .Where(dp => dp.DeviceId == request.DeviceId && dp.IsActive)
            .OrderByDescending(dp => dp.EffectiveFrom)
            .ToListAsync(cancellationToken);

        if (!_allPrices.Any())
        {
            throw new InvalidOperationException("No energy prices configured for this device. Please set sell and buy prices first using the EnergyRevenue API.");
        }

        _currency = _allPrices.First().Currency;

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

        // Calculate totals (prices are already calculated per data point)
        var totalBuyKWh = dataPoints.Sum(d => d.Buy);
        var totalSellKWh = dataPoints.Sum(d => d.Sell);
        var totalBuyPrice = dataPoints.Sum(d => d.Buy * d.BuyPriceUsed);
        var totalSellPrice = dataPoints.Sum(d => d.Sell * d.SellPriceUsed);
        var totalNetPrice = totalSellPrice - totalBuyPrice;

        // Get current average prices for display
        var currentPrices = GetAveragePricesForDate(DateTime.UtcNow);

        return new EnergyRevenueDto
        {
            Period = period,
            BuyPricePerKWh = currentPrices.BuyPrice,
            SellPricePerKWh = currentPrices.SellPrice,
            Currency = _currency,
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
    /// Get prices effective for a specific date and hour
    /// </summary>
    private List<DevicePrice> GetEffectivePricesForDateTime(DateTime dateTime)
    {
        var dateTimeOffset = new DateTimeOffset(dateTime, TimeSpan.Zero);
        var hour = dateTime.Hour;

        return _allPrices
            .Where(dp => dp.EffectiveFrom <= dateTimeOffset &&
                        (dp.EffectiveTo == null || dp.EffectiveTo > dateTimeOffset))
            .Where(dp =>
                // No hour restriction (all day)
                (!dp.BeginHour.HasValue && !dp.EndHour.HasValue) ||
                // Has hour restriction and current hour falls within
                (dp.BeginHour.HasValue && dp.EndHour.HasValue &&
                 hour >= dp.BeginHour.Value && hour < dp.EndHour.Value))
            .ToList();
    }

    /// <summary>
    /// Get average sell and buy prices for a specific date
    /// If multiple prices exist with hour ranges, calculate weighted average
    /// </summary>
    private (double SellPrice, double BuyPrice) GetAveragePricesForDate(DateTime date)
    {
        var dateTimeOffset = new DateTimeOffset(date.Date, TimeSpan.Zero);

        // Get all prices effective on this date
        var effectivePrices = _allPrices
            .Where(dp => dp.EffectiveFrom.Date <= date.Date &&
                        (dp.EffectiveTo == null || dp.EffectiveTo.Value.Date >= date.Date))
            .ToList();

        if (!effectivePrices.Any())
        {
            // Fallback to most recent price before this date
            var fallbackPrice = _allPrices
                .Where(dp => dp.EffectiveFrom.Date <= date.Date)
                .OrderByDescending(dp => dp.EffectiveFrom)
                .FirstOrDefault();

            if (fallbackPrice != null)
            {
                return ((double)fallbackPrice.SellPrice, (double)fallbackPrice.BuyPrice);
            }

            // If no price found, return 0
            return (0, 0);
        }

        // Separate all-day prices from hour-specific prices
        var allDayPrices = effectivePrices.Where(p => !p.BeginHour.HasValue && !p.EndHour.HasValue).ToList();
        var hourlyPrices = effectivePrices.Where(p => p.BeginHour.HasValue && p.EndHour.HasValue).ToList();

        // If only all-day prices exist, average them
        if (!hourlyPrices.Any())
        {
            var avgSell = allDayPrices.Average(p => (double)p.SellPrice);
            var avgBuy = allDayPrices.Average(p => (double)p.BuyPrice);
            return (avgSell, avgBuy);
        }

        // Calculate weighted average based on hours covered
        double totalSell = 0;
        double totalBuy = 0;
        int totalHours = 0;

        foreach (var price in hourlyPrices)
        {
            var hours = price.EndHour!.Value - price.BeginHour!.Value;
            totalSell += (double)price.SellPrice * hours;
            totalBuy += (double)price.BuyPrice * hours;
            totalHours += hours;
        }

        // Fill remaining hours with all-day price if available
        if (totalHours < 24 && allDayPrices.Any())
        {
            var remainingHours = 24 - totalHours;
            var allDaySell = allDayPrices.Average(p => (double)p.SellPrice);
            var allDayBuy = allDayPrices.Average(p => (double)p.BuyPrice);
            totalSell += allDaySell * remainingHours;
            totalBuy += allDayBuy * remainingHours;
            totalHours = 24;
        }

        if (totalHours == 0)
            return (0, 0);

        return (totalSell / totalHours, totalBuy / totalHours);
    }

    /// <summary>
    /// Get price for a specific hour on a specific date
    /// </summary>
    private (double SellPrice, double BuyPrice) GetPriceForHour(DateTime dateTime)
    {
        var effectivePrices = GetEffectivePricesForDateTime(dateTime);

        if (!effectivePrices.Any())
        {
            // Fallback to average for the date
            return GetAveragePricesForDate(dateTime.Date);
        }

        // If multiple prices match, average them
        var avgSell = effectivePrices.Average(p => (double)p.SellPrice);
        var avgBuy = effectivePrices.Average(p => (double)p.BuyPrice);
        return (avgSell, avgBuy);
    }

    /// <summary>
    /// Get hourly revenue data for a single day (up to 24 data points, stops at current hour if today)
    /// Calculates revenue from PowerFlow data (5-minute intervals)
    /// Returns empty array if no data exists for this day
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

        // If no data exists for this period, return empty array
        if (!powerFlowData.Any())
        {
            return new List<EnergyRevenueDataPoint>();
        }

        // Apply interpolation for missing data (if gaps > 5 minutes)
        var interpolatedData = InterpolatePowerFlowGaps(powerFlowData, TimeSpan.FromMinutes(5));

        var result = new List<EnergyRevenueDataPoint>();
        var now = DateTime.UtcNow;
        var today = DateTime.UtcNow.Date;

        // Determine last hour to include (don't include future hours)
        int lastHour = 23;
        if (dayStart.Date == today)
        {
            lastHour = now.Hour; // Stop at current hour if today
        }
        else if (dayStart.Date > today)
        {
            // Future date - return empty array
            return new List<EnergyRevenueDataPoint>();
        }

        // Generate hourly data points up to lastHour
        for (int hour = 0; hour <= lastHour; hour++)
        {
            var hourStart = dayStart.AddHours(hour);
            var hourEnd = hourStart.AddHours(1);

            // Get price for this specific hour
            var (sellPrice, buyPrice) = GetPriceForHour(hourStart);

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

            // Calculate revenue for this hour using hour-specific prices
            var net = sellEnergyKWh - buyEnergyKWh;
            var price = (sellEnergyKWh * sellPrice) - (buyEnergyKWh * buyPrice);

            result.Add(new EnergyRevenueDataPoint
            {
                Timestamp = hourStart.ToString("HH:mm"),
                Buy = Math.Round(buyEnergyKWh, 3),
                Sell = Math.Round(sellEnergyKWh, 3),
                Net = Math.Round(net, 3),
                Price = Math.Round(price, 2),
                BuyPriceUsed = buyPrice,
                SellPriceUsed = sellPrice
            });
        }

        return result;
    }

    /// <summary>
    /// Interpolate missing power flow data using simple averaging
    /// When gaps exceed the expected sampling interval, fill with average of surrounding samples
    /// </summary>
    private List<DevicePowerData> InterpolatePowerFlowGaps(
        List<DevicePowerData> data,
        TimeSpan expectedInterval)
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
    /// Get daily revenue data for a specific week (up to 7 days, stops at today)
    /// Returns empty array if no data exists for this week
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

        // If no data exists for this period, return empty array
        if (!dailyData.Any())
        {
            return new List<EnergyRevenueDataPoint>();
        }

        // Group by date and take the most recent entry for each day
        var latestDailyData = dailyData
            .GroupBy(d => d.DataDateTime.Date)
            .Select(g => g.OrderByDescending(d => d.DataDateTime).First())
            .ToDictionary(d => d.DataDateTime.Date);

        var result = new List<EnergyRevenueDataPoint>();
        var today = DateTime.UtcNow.Date;

        // Generate days in the week up to today (don't include future days)
        for (int i = 0; i < 7; i++)
        {
            var currentDate = weekStartUtc.AddDays(i);

            // Skip future dates
            if (currentDate.Date > today)
            {
                break;
            }

            // Get average prices for this specific day
            var (sellPrice, buyPrice) = GetAveragePricesForDate(currentDate);

            if (latestDailyData.TryGetValue(currentDate.Date, out var data))
            {
                var buy = data.GridImportEnergyKWh ?? 0;
                var sell = data.GridExportEnergyKWh ?? 0;
                var net = sell - buy;
                var price = (sell * sellPrice) - (buy * buyPrice);

                result.Add(new EnergyRevenueDataPoint
                {
                    Timestamp = currentDate.ToString("yyyy-MM-dd"),
                    Buy = Math.Round(buy, 2),
                    Sell = Math.Round(sell, 2),
                    Net = Math.Round(net, 2),
                    Price = Math.Round(price, 2),
                    BuyPriceUsed = buyPrice,
                    SellPriceUsed = sellPrice
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
                    Price = 0,
                    BuyPriceUsed = buyPrice,
                    SellPriceUsed = sellPrice
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Get daily revenue data for a specific month (stops at today if current month)
    /// Returns empty array if no data exists for this month
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

        // If no data exists for this period, return empty array
        if (!dailyData.Any())
        {
            return new List<EnergyRevenueDataPoint>();
        }

        // Group by date and take the most recent entry for each day
        var latestDailyData = dailyData
            .GroupBy(d => d.DataDateTime.Date)
            .Select(g => g.OrderByDescending(d => d.DataDateTime).First())
            .ToDictionary(d => d.DataDateTime.Date);

        var result = new List<EnergyRevenueDataPoint>();
        var today = DateTime.UtcNow.Date;
        var currentDate = monthStart;
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var lastDateInMonth = monthStart.AddDays(daysInMonth - 1);

        // Don't include future dates - stop at today if this is current or future month
        var lastDate = lastDateInMonth < today ? lastDateInMonth : today;

        while (currentDate <= lastDate)
        {
            // Get average prices for this specific day
            var (sellPrice, buyPrice) = GetAveragePricesForDate(currentDate);

            if (latestDailyData.TryGetValue(currentDate, out var data))
            {
                var buy = data.GridImportEnergyKWh ?? 0;
                var sell = data.GridExportEnergyKWh ?? 0;
                var net = sell - buy;
                var price = (sell * sellPrice) - (buy * buyPrice);

                result.Add(new EnergyRevenueDataPoint
                {
                    Timestamp = currentDate.ToString("yyyy-MM-dd"),
                    Buy = Math.Round(buy, 2),
                    Sell = Math.Round(sell, 2),
                    Net = Math.Round(net, 2),
                    Price = Math.Round(price, 2),
                    BuyPriceUsed = buyPrice,
                    SellPriceUsed = sellPrice
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
                    Price = 0,
                    BuyPriceUsed = buyPrice,
                    SellPriceUsed = sellPrice
                });
            }

            currentDate = currentDate.AddDays(1);
        }

        return result;
    }

    /// <summary>
    /// Get monthly aggregated revenue data for a specific year (stops at current month if current year)
    /// Returns empty array if no data exists for this year
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

        // If no data exists for this period, return empty array
        if (!dailyData.Any())
        {
            return new List<EnergyRevenueDataPoint>();
        }

        // Group by date and take the most recent entry for each day
        var latestDailyData = dailyData
            .GroupBy(d => d.DataDateTime.Date)
            .Select(g => g.OrderByDescending(d => d.DataDateTime).First())
            .ToList();

        var result = new List<EnergyRevenueDataPoint>();
        var today = DateTime.UtcNow.Date;
        var currentYear = today.Year;
        var currentMonth = today.Month;

        // Determine last month to include (don't include future months)
        int lastMonth = 12;
        if (year == currentYear)
        {
            lastMonth = currentMonth; // Stop at current month if this year
        }
        else if (year > currentYear)
        {
            // Future year - return empty array
            return new List<EnergyRevenueDataPoint>();
        }

        // Generate months up to lastMonth
        for (int month = 1; month <= lastMonth; month++)
        {
            var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);

            // Get daily data for this month
            var monthDailyData = latestDailyData
                .Where(d => d.DataDateTime >= monthStart && d.DataDateTime < monthEnd)
                .ToList();

            if (monthDailyData.Any())
            {
                // Calculate revenue for each day with its specific price, then sum
                double totalBuy = 0;
                double totalSell = 0;
                double totalPrice = 0;
                double avgBuyPrice = 0;
                double avgSellPrice = 0;
                int priceCount = 0;

                foreach (var dayData in monthDailyData)
                {
                    var (sellPrice, buyPrice) = GetAveragePricesForDate(dayData.DataDateTime.Date);
                    var buy = dayData.GridImportEnergyKWh ?? 0;
                    var sell = dayData.GridExportEnergyKWh ?? 0;

                    totalBuy += buy;
                    totalSell += sell;
                    totalPrice += (sell * sellPrice) - (buy * buyPrice);
                    avgBuyPrice += buyPrice;
                    avgSellPrice += sellPrice;
                    priceCount++;
                }

                // Average price for the month (for display)
                avgBuyPrice = priceCount > 0 ? avgBuyPrice / priceCount : 0;
                avgSellPrice = priceCount > 0 ? avgSellPrice / priceCount : 0;

                var net = totalSell - totalBuy;

                result.Add(new EnergyRevenueDataPoint
                {
                    Timestamp = $"{year}-{month:D2}",
                    Buy = Math.Round(totalBuy, 2),
                    Sell = Math.Round(totalSell, 2),
                    Net = Math.Round(net, 2),
                    Price = Math.Round(totalPrice, 2),
                    BuyPriceUsed = avgBuyPrice,
                    SellPriceUsed = avgSellPrice
                });
            }
            else
            {
                // No data for this month - get average prices for mid-month
                var midMonth = new DateTime(year, month, 15, 0, 0, 0, DateTimeKind.Utc);
                var (sellPrice, buyPrice) = GetAveragePricesForDate(midMonth);

                result.Add(new EnergyRevenueDataPoint
                {
                    Timestamp = $"{year}-{month:D2}",
                    Buy = 0,
                    Sell = 0,
                    Net = 0,
                    Price = 0,
                    BuyPriceUsed = buyPrice,
                    SellPriceUsed = sellPrice
                });
            }
        }

        return result;
    }
}
