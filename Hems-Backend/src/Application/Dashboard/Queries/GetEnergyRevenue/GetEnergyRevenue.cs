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
    private readonly IDemoDataService _demoDataService;
    private readonly IDemoSimulationDataService _demoSimulationService;

    private Guid _deviceId;
    private string _currency = "EUR";

    public GetEnergyRevenueQueryHandler(
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

    public async Task<EnergyRevenueDto> Handle(GetEnergyRevenueQuery request, CancellationToken cancellationToken)
    {
        _deviceId = request.DeviceId;

        // Check device access
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Device access denied");
        }

        var period = request.Period.ToLower();

        // Check if this is a demo device
        var isDemoDevice = await _demoDataService.IsDemoDeviceAsync(request.DeviceId, cancellationToken);

        if (isDemoDevice)
        {
            return HandleDemoDeviceRevenue(request, period);
        }

        // For real devices: query database
        // Map local DeviceId to ThingsBoardDeviceId
        var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(request.DeviceId, cancellationToken);
        if (thingsBoardDeviceId == null)
        {
            throw new ArgumentException($"Device with ID {request.DeviceId} not found or has no ThingsBoard mapping.");
        }

        // Get current active prices for display (header info)
        var currentPrices = await GetCurrentActivePricesAsync(cancellationToken);
        if (currentPrices.Any())
        {
            _currency = currentPrices.First().Currency;
        }

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

        // Calculate totals
        var totalBuyKWh = dataPoints.Sum(d => d.Buy);
        var totalSellKWh = dataPoints.Sum(d => d.Sell);
        var totalBuyPrice = dataPoints.Sum(d => d.Buy * d.BuyPriceUsed);
        var totalSellPrice = dataPoints.Sum(d => d.Sell * d.SellPriceUsed);
        var totalNetPrice = totalSellPrice - totalBuyPrice;

        // Calculate average prices used (for display)
        var avgBuyPrice = CalculateWeightedAveragePrice(dataPoints, d => d.Buy, d => d.BuyPriceUsed);
        var avgSellPrice = CalculateWeightedAveragePrice(dataPoints, d => d.Sell, d => d.SellPriceUsed);

        return new EnergyRevenueDto
        {
            Period = period,
            BuyPricePerKWh = avgBuyPrice,
            SellPricePerKWh = avgSellPrice,
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

    #region Demo Device Revenue

    /// <summary>
    /// Handle revenue calculation for demo devices using simulated data
    /// </summary>
    private EnergyRevenueDto HandleDemoDeviceRevenue(GetEnergyRevenueQuery request, string period)
    {
        var priceSettings = _demoSimulationService.GetDemoPrices(_deviceId);
        _currency = priceSettings.Currency;

        List<EnergyRevenueDataPoint> dataPoints = period switch
        {
            "day" => GetDemoDayData(request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow)),
            "week" => GetDemoWeekData(request.Year ?? DateTime.UtcNow.Year, request.Week ?? GetIsoWeekNumber(DateTime.UtcNow)),
            "month" => GetDemoMonthData(request.Year ?? DateTime.UtcNow.Year, request.Month ?? DateTime.UtcNow.Month),
            "year" => GetDemoYearData(request.Year ?? DateTime.UtcNow.Year),
            _ => throw new ArgumentException($"Invalid period: {period}")
        };

        // Calculate totals
        var totalBuyPrice = dataPoints.Sum(d => d.Buy * d.BuyPriceUsed);
        var totalSellPrice = dataPoints.Sum(d => d.Sell * d.SellPriceUsed);
        var totalNetPrice = totalSellPrice - totalBuyPrice;

        var avgBuyPrice = CalculateWeightedAveragePrice(dataPoints, d => d.Buy, d => d.BuyPriceUsed);
        var avgSellPrice = CalculateWeightedAveragePrice(dataPoints, d => d.Sell, d => d.SellPriceUsed);

        return new EnergyRevenueDto
        {
            Period = period,
            BuyPricePerKWh = avgBuyPrice,
            SellPricePerKWh = avgSellPrice,
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
    /// Get simulated hourly revenue data for a single day (demo device)
    /// </summary>
    private List<EnergyRevenueDataPoint> GetDemoDayData(DateOnly date)
    {
        var dayStart = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;

        // Don't return future data
        if (dayStart.Date > today)
        {
            return new List<EnergyRevenueDataPoint>();
        }

        var dailyEnergy = _demoSimulationService.GetDailyEnergy(dayStart);
        var result = new List<EnergyRevenueDataPoint>();

        int lastHour = dayStart.Date == today ? now.Hour : 23;

        // Generate hourly distribution based on typical solar pattern
        for (int hour = 0; hour <= lastHour; hour++)
        {
            var (sellPrice, buyPrice) = _demoSimulationService.GetPriceForHour(_deviceId, hour);

            // Simulate hourly energy distribution
            var hourlyFactor = GetHourlyEnergyFactor(hour);
            var buyEnergy = dailyEnergy.GridImportEnergyKWh * hourlyFactor.ImportFactor / 24.0;
            var sellEnergy = dailyEnergy.GridExportEnergyKWh * hourlyFactor.ExportFactor / 24.0;

            var net = sellEnergy - buyEnergy;
            var price = (sellEnergy * (double)sellPrice) - (buyEnergy * (double)buyPrice);

            result.Add(new EnergyRevenueDataPoint
            {
                Timestamp = $"{hour:D2}:00",
                Buy = Math.Round(buyEnergy, 3),
                Sell = Math.Round(sellEnergy, 3),
                Net = Math.Round(net, 3),
                Price = Math.Round(price, 2),
                BuyPriceUsed = (double)buyPrice,
                SellPriceUsed = (double)sellPrice
            });
        }

        return result;
    }

    /// <summary>
    /// Get simulated daily revenue data for a week (demo device)
    /// </summary>
    private List<EnergyRevenueDataPoint> GetDemoWeekData(int year, int week)
    {
        var weekStart = ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
        var weekStartUtc = DateTime.SpecifyKind(weekStart, DateTimeKind.Utc);
        var today = DateTime.UtcNow.Date;

        var result = new List<EnergyRevenueDataPoint>();

        for (int i = 0; i < 7; i++)
        {
            var currentDate = weekStartUtc.AddDays(i);
            if (currentDate.Date > today) break;

            var dailyEnergy = _demoSimulationService.GetDailyEnergy(currentDate);
            var (sellPrice, buyPrice) = _demoSimulationService.GetWeightedDailyAveragePrice(_deviceId);

            var buy = dailyEnergy.GridImportEnergyKWh;
            var sell = dailyEnergy.GridExportEnergyKWh;
            var net = sell - buy;
            var price = (sell * (double)sellPrice) - (buy * (double)buyPrice);

            result.Add(new EnergyRevenueDataPoint
            {
                Timestamp = currentDate.ToString("yyyy-MM-dd"),
                Buy = Math.Round(buy, 2),
                Sell = Math.Round(sell, 2),
                Net = Math.Round(net, 2),
                Price = Math.Round(price, 2),
                BuyPriceUsed = (double)buyPrice,
                SellPriceUsed = (double)sellPrice
            });
        }

        return result;
    }

    /// <summary>
    /// Get simulated daily revenue data for a month (demo device)
    /// </summary>
    private List<EnergyRevenueDataPoint> GetDemoMonthData(int year, int month)
    {
        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var today = DateTime.UtcNow.Date;

        var result = new List<EnergyRevenueDataPoint>();

        for (int day = 1; day <= daysInMonth; day++)
        {
            var currentDate = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
            if (currentDate.Date > today) break;

            var dailyEnergy = _demoSimulationService.GetDailyEnergy(currentDate);
            var (sellPrice, buyPrice) = _demoSimulationService.GetWeightedDailyAveragePrice(_deviceId);

            var buy = dailyEnergy.GridImportEnergyKWh;
            var sell = dailyEnergy.GridExportEnergyKWh;
            var net = sell - buy;
            var price = (sell * (double)sellPrice) - (buy * (double)buyPrice);

            result.Add(new EnergyRevenueDataPoint
            {
                Timestamp = currentDate.ToString("yyyy-MM-dd"),
                Buy = Math.Round(buy, 2),
                Sell = Math.Round(sell, 2),
                Net = Math.Round(net, 2),
                Price = Math.Round(price, 2),
                BuyPriceUsed = (double)buyPrice,
                SellPriceUsed = (double)sellPrice
            });
        }

        return result;
    }

    /// <summary>
    /// Get simulated monthly revenue data for a year (demo device)
    /// </summary>
    private List<EnergyRevenueDataPoint> GetDemoYearData(int year)
    {
        var today = DateTime.UtcNow.Date;
        var currentYear = today.Year;
        var currentMonth = today.Month;

        if (year > currentYear)
        {
            return new List<EnergyRevenueDataPoint>();
        }

        var result = new List<EnergyRevenueDataPoint>();
        int lastMonth = year == currentYear ? currentMonth : 12;

        for (int month = 1; month <= lastMonth; month++)
        {
            var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var lastDay = year == currentYear && month == currentMonth ? today.Day : daysInMonth;

            double totalBuy = 0;
            double totalSell = 0;

            for (int day = 1; day <= lastDay; day++)
            {
                var currentDate = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
                var dailyEnergy = _demoSimulationService.GetDailyEnergy(currentDate);
                totalBuy += dailyEnergy.GridImportEnergyKWh;
                totalSell += dailyEnergy.GridExportEnergyKWh;
            }

            var (sellPrice, buyPrice) = _demoSimulationService.GetWeightedDailyAveragePrice(_deviceId);
            var net = totalSell - totalBuy;
            var price = (totalSell * (double)sellPrice) - (totalBuy * (double)buyPrice);

            result.Add(new EnergyRevenueDataPoint
            {
                Timestamp = $"{year}-{month:D2}",
                Buy = Math.Round(totalBuy, 2),
                Sell = Math.Round(totalSell, 2),
                Net = Math.Round(net, 2),
                Price = Math.Round(price, 2),
                BuyPriceUsed = (double)buyPrice,
                SellPriceUsed = (double)sellPrice
            });
        }

        return result;
    }

    /// <summary>
    /// Get hourly energy distribution factors for realistic simulation
    /// </summary>
    private static (double ImportFactor, double ExportFactor) GetHourlyEnergyFactor(int hour)
    {
        // Import is higher at night/evening, export is higher during solar hours
        return hour switch
        {
            >= 0 and < 6 => (1.8, 0.0),      // Night: high import, no export
            >= 6 and < 9 => (1.5, 0.3),      // Morning: moderate import, low export
            >= 9 and < 12 => (0.5, 2.5),     // Late morning: low import, high export
            >= 12 and < 15 => (0.3, 3.0),    // Midday: minimal import, peak export
            >= 15 and < 18 => (0.6, 2.0),    // Afternoon: low import, good export
            >= 18 and < 21 => (2.0, 0.2),    // Evening: high import, minimal export
            _ => (1.5, 0.0)                  // Night: moderate import, no export
        };
    }

    private static int GetIsoWeekNumber(DateTime date)
    {
        return ISOWeek.GetWeekOfYear(date);
    }

    #endregion

    /// <summary>
    /// Calculate weighted average price based on energy consumed/produced
    /// </summary>
    private double CalculateWeightedAveragePrice(
        List<EnergyRevenueDataPoint> dataPoints,
        Func<EnergyRevenueDataPoint, double> energySelector,
        Func<EnergyRevenueDataPoint, double> priceSelector)
    {
        var totalEnergy = dataPoints.Sum(energySelector);
        if (totalEnergy <= 0) return 0;

        var weightedSum = dataPoints.Sum(d => energySelector(d) * priceSelector(d));
        return Math.Round(weightedSum / totalEnergy, 4);
    }

    /// <summary>
    /// Get current active prices for the device
    /// </summary>
    private async Task<List<EnergyPriceHistory>> GetCurrentActivePricesAsync(CancellationToken cancellationToken)
    {
        return await _context.EnergyPriceHistory
            .Where(p => p.DeviceId == _deviceId && p.IsActive)
            .OrderBy(p => p.BeginHour)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get prices that were effective at a specific date.
    /// Finds the most recent price setting event before or on the target date.
    /// </summary>
    private async Task<List<EnergyPriceHistory>> GetPricesEffectiveAtAsync(DateTime date, CancellationToken cancellationToken)
    {
        var dateOffset = new DateTimeOffset(date.Date.AddDays(1).AddTicks(-1), TimeSpan.Zero); // End of the day

        // Find the most recent price setting event before or on the target date
        var mostRecentCreated = await _context.EnergyPriceHistory
            .Where(p => p.DeviceId == _deviceId && p.Created <= dateOffset)
            .OrderByDescending(p => p.Created)
            .Select(p => p.Created)
            .FirstOrDefaultAsync(cancellationToken);

        if (mostRecentCreated == default)
        {
            return new List<EnergyPriceHistory>();
        }

        // Get all prices from that setting event (they share the same Created timestamp)
        return await _context.EnergyPriceHistory
            .Where(p => p.DeviceId == _deviceId && p.Created == mostRecentCreated)
            .OrderBy(p => p.BeginHour)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get the price for a specific hour from a list of prices (time-of-day pricing)
    /// </summary>
    private (double SellPrice, double BuyPrice) GetPriceForHour(List<EnergyPriceHistory> prices, int hour)
    {
        if (!prices.Any())
        {
            return (0, 0);
        }

        // Find prices that cover this hour
        var matchingPrices = prices
            .Where(p =>
                // No hour restriction (all day)
                (!p.BeginHour.HasValue && !p.EndHour.HasValue) ||
                // Has hour restriction and hour falls within
                (p.BeginHour.HasValue && p.EndHour.HasValue &&
                 hour >= p.BeginHour.Value && hour < p.EndHour.Value))
            .ToList();

        if (!matchingPrices.Any())
        {
            // No matching time slot - use first price as fallback
            var fallback = prices.First();
            return ((double)fallback.SellPrice, (double)fallback.BuyPrice);
        }

        // Average if multiple match (shouldn't happen with proper config, but handle it)
        var avgSell = matchingPrices.Average(p => (double)p.SellPrice);
        var avgBuy = matchingPrices.Average(p => (double)p.BuyPrice);
        return (avgSell, avgBuy);
    }

    /// <summary>
    /// Calculate weighted average price for a full day (all 24 hours)
    /// Used when we only have daily totals without hourly breakdown
    /// </summary>
    private (double SellPrice, double BuyPrice) GetWeightedDailyAveragePrice(List<EnergyPriceHistory> prices)
    {
        if (!prices.Any())
        {
            return (0, 0);
        }

        // Separate all-day prices from hour-specific prices
        var allDayPrices = prices.Where(p => !p.BeginHour.HasValue && !p.EndHour.HasValue).ToList();
        var hourlyPrices = prices.Where(p => p.BeginHour.HasValue && p.EndHour.HasValue).ToList();

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
    /// Get hourly revenue data for a single day using PowerFlow data (5-minute intervals)
    /// Returns empty array if no data exists for this day
    /// </summary>
    private async Task<List<EnergyRevenueDataPoint>> GetDayDataAsync(Guid thingsBoardDeviceId, DateOnly date, CancellationToken cancellationToken)
    {
        var dayStart = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        // Get prices effective on this specific date
        var effectivePrices = await GetPricesEffectiveAtAsync(dayStart, cancellationToken);
        var hasPrices = effectivePrices.Any();

        // Get all PowerFlow data for the day (5-minute intervals)
        var powerFlowData = await _context.DevicePowerData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DeviceLocalTime >= dayStart &&
                       x.DeviceLocalTime < dayEnd &&
                       x.DataType == DataTypes.PowerFlow)
            .OrderBy(x => x.DeviceLocalTime)
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
            lastHour = now.Hour;
        }
        else if (dayStart.Date > today)
        {
            return new List<EnergyRevenueDataPoint>();
        }

        // Generate hourly data points
        for (int hour = 0; hour <= lastHour; hour++)
        {
            var hourStart = dayStart.AddHours(hour);
            var hourEnd = hourStart.AddHours(1);

            // Get price for this specific hour
            var (sellPrice, buyPrice) = hasPrices
                ? GetPriceForHour(effectivePrices, hour)
                : (0.0, 0.0);

            // Get all power samples for this hour
            var hourSamples = interpolatedData
                .Where(x => x.DeviceLocalTime >= hourStart && x.DeviceLocalTime < hourEnd)
                .ToList();

            double buyEnergyKWh = 0;
            double sellEnergyKWh = 0;

            if (hourSamples.Any())
            {
                const double timeIntervalHours = 5.0 / 60.0; // 5 minutes

                foreach (var sample in hourSamples)
                {
                    var gridPowerW = sample.GridTotalPowerW ?? 0;

                    if (gridPowerW > 0)
                    {
                        buyEnergyKWh += (gridPowerW * timeIntervalHours) / 1000.0;
                    }
                    else if (gridPowerW < 0)
                    {
                        sellEnergyKWh += (Math.Abs(gridPowerW) * timeIntervalHours) / 1000.0;
                    }
                }
            }

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
    /// Interpolate missing power flow data
    /// </summary>
    private List<DevicePowerData> InterpolatePowerFlowGaps(List<DevicePowerData> data, TimeSpan expectedInterval)
    {
        if (data.Count < 2)
            return data;

        var interpolated = new List<DevicePowerData>();

        for (int i = 0; i < data.Count; i++)
        {
            interpolated.Add(data[i]);

            if (i < data.Count - 1)
            {
                var current = data[i];
                var next = data[i + 1];
                var gap = next.DeviceLocalTime - current.DeviceLocalTime;

                if (gap > expectedInterval * 2)
                {
                    var missingIntervals = (int)(gap.TotalMinutes / expectedInterval.TotalMinutes) - 1;

                    for (int j = 1; j <= missingIntervals; j++)
                    {
                        var interpolatedSample = new DevicePowerData
                        {
                            ThingsBoardDeviceId = current.ThingsBoardDeviceId,
                            DeviceLocalTime = current.DeviceLocalTime.AddMinutes(expectedInterval.TotalMinutes * j),
                            DataType = current.DataType,
                            MessageType = current.MessageType,
                            PhaseType = current.PhaseType,
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

    private double? InterpolatePowerValue(double? before, double? after)
    {
        if (!before.HasValue || !after.HasValue)
            return before ?? after;

        return (before.Value + after.Value) / 2.0;
    }

    /// <summary>
    /// Get daily revenue data for a specific week using DailyCumulativeEnergy
    /// </summary>
    private async Task<List<EnergyRevenueDataPoint>> GetWeekDataAsync(Guid thingsBoardDeviceId, int year, int week, CancellationToken cancellationToken)
    {
        var weekStart = ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
        var weekStartUtc = DateTime.SpecifyKind(weekStart, DateTimeKind.Utc);
        var weekEnd = weekStartUtc.AddDays(7);

        var dailyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DeviceLocalTime >= weekStartUtc &&
                       x.DeviceLocalTime < weekEnd &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .OrderBy(x => x.DeviceLocalTime)
            .ToListAsync(cancellationToken);

        if (!dailyData.Any())
        {
            return new List<EnergyRevenueDataPoint>();
        }

        // Group by date and take the most recent entry for each day
        var latestDailyData = dailyData
            .GroupBy(d => d.DeviceLocalTime.Date)
            .Select(g => g.OrderByDescending(d => d.DeviceLocalTime).First())
            .ToDictionary(d => d.DeviceLocalTime.Date);

        var result = new List<EnergyRevenueDataPoint>();
        var today = DateTime.UtcNow.Date;

        for (int i = 0; i < 7; i++)
        {
            var currentDate = weekStartUtc.AddDays(i);

            if (currentDate.Date > today)
                break;

            // Get prices effective on this specific day
            var effectivePrices = await GetPricesEffectiveAtAsync(currentDate, cancellationToken);
            var (sellPrice, buyPrice) = GetWeightedDailyAveragePrice(effectivePrices);

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
    /// Get daily revenue data for a specific month using DailyCumulativeEnergy
    /// For the current day (if today), uses PowerFlow data for partial day calculation
    /// </summary>
    private async Task<List<EnergyRevenueDataPoint>> GetMonthDataAsync(Guid thingsBoardDeviceId, int year, int month, CancellationToken cancellationToken)
    {
        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);
        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;

        var dailyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DeviceLocalTime >= monthStart &&
                       x.DeviceLocalTime < monthEnd &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .OrderBy(x => x.DeviceLocalTime)
            .ToListAsync(cancellationToken);

        // Group by date and take the most recent entry for each day
        var latestDailyData = dailyData
            .GroupBy(d => d.DeviceLocalTime.Date)
            .Select(g => g.OrderByDescending(d => d.DeviceLocalTime).First())
            .ToDictionary(d => d.DeviceLocalTime.Date);

        var result = new List<EnergyRevenueDataPoint>();
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var lastDateInMonth = monthStart.AddDays(daysInMonth - 1);
        var lastDate = lastDateInMonth < today ? lastDateInMonth : today;
        var currentDate = monthStart;

        while (currentDate <= lastDate)
        {
            // Get prices effective on this specific day
            var effectivePrices = await GetPricesEffectiveAtAsync(currentDate, cancellationToken);
            var (sellPrice, buyPrice) = GetWeightedDailyAveragePrice(effectivePrices);

            double buy = 0;
            double sell = 0;

            // Check if this is today (partial day) - use PowerFlow data
            if (currentDate.Date == today)
            {
                var partialDayData = await GetPartialDayEnergyAsync(thingsBoardDeviceId, today, cancellationToken);
                buy = partialDayData.Buy;
                sell = partialDayData.Sell;
            }
            else if (latestDailyData.TryGetValue(currentDate.Date, out var data))
            {
                buy = data.GridImportEnergyKWh ?? 0;
                sell = data.GridExportEnergyKWh ?? 0;
            }

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

            currentDate = currentDate.AddDays(1);
        }

        return result;
    }

    /// <summary>
    /// Get energy totals for a partial day (today) from PowerFlow data
    /// Sums from midnight to current hour
    /// </summary>
    private async Task<(double Buy, double Sell)> GetPartialDayEnergyAsync(Guid thingsBoardDeviceId, DateTime date, CancellationToken cancellationToken)
    {
        var dayStart = date.Date;
        var now = DateTime.UtcNow;

        var powerFlowData = await _context.DevicePowerData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DeviceLocalTime >= dayStart &&
                       x.DeviceLocalTime <= now &&
                       x.DataType == DataTypes.PowerFlow)
            .OrderBy(x => x.DeviceLocalTime)
            .ToListAsync(cancellationToken);

        if (!powerFlowData.Any())
        {
            return (0, 0);
        }

        var interpolatedData = InterpolatePowerFlowGaps(powerFlowData, TimeSpan.FromMinutes(5));

        double buyEnergyKWh = 0;
        double sellEnergyKWh = 0;
        const double timeIntervalHours = 5.0 / 60.0;

        foreach (var sample in interpolatedData)
        {
            var gridPowerW = sample.GridTotalPowerW ?? 0;

            if (gridPowerW > 0)
            {
                buyEnergyKWh += (gridPowerW * timeIntervalHours) / 1000.0;
            }
            else if (gridPowerW < 0)
            {
                sellEnergyKWh += (Math.Abs(gridPowerW) * timeIntervalHours) / 1000.0;
            }
        }

        return (buyEnergyKWh, sellEnergyKWh);
    }

    /// <summary>
    /// Get monthly aggregated revenue data for a specific year using DailyCumulativeEnergy
    /// </summary>
    private async Task<List<EnergyRevenueDataPoint>> GetYearDataAsync(Guid thingsBoardDeviceId, int year, CancellationToken cancellationToken)
    {
        var yearStart = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearEnd = yearStart.AddYears(1);
        var today = DateTime.UtcNow.Date;
        var currentYear = today.Year;
        var currentMonth = today.Month;

        if (year > currentYear)
        {
            return new List<EnergyRevenueDataPoint>();
        }

        var dailyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DeviceLocalTime >= yearStart &&
                       x.DeviceLocalTime < yearEnd &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .OrderBy(x => x.DeviceLocalTime)
            .ToListAsync(cancellationToken);

        // Group by date and take the most recent entry for each day
        var latestDailyData = dailyData
            .GroupBy(d => d.DeviceLocalTime.Date)
            .Select(g => g.OrderByDescending(d => d.DeviceLocalTime).First())
            .ToList();

        var result = new List<EnergyRevenueDataPoint>();
        int lastMonth = year == currentYear ? currentMonth : 12;

        for (int month = 1; month <= lastMonth; month++)
        {
            var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);

            // Get daily data for this month
            var monthDailyData = latestDailyData
                .Where(d => d.DeviceLocalTime >= monthStart && d.DeviceLocalTime < monthEnd)
                .ToList();

            // Calculate energy totals
            double totalBuy = monthDailyData.Sum(d => d.GridImportEnergyKWh ?? 0);
            double totalSell = monthDailyData.Sum(d => d.GridExportEnergyKWh ?? 0);

            // If this is the current month, add partial day data for today
            if (year == currentYear && month == currentMonth)
            {
                var partialDayData = await GetPartialDayEnergyAsync(thingsBoardDeviceId, today, cancellationToken);
                totalBuy += partialDayData.Buy;
                totalSell += partialDayData.Sell;
            }

            // Calculate weighted average price for the month
            // Get all unique price settings used during this month
            var monthPriceInfo = await CalculateMonthlyPriceInfoAsync(monthDailyData, monthStart, monthEnd, cancellationToken);

            var totalPrice = (totalSell * monthPriceInfo.AvgSellPrice) - (totalBuy * monthPriceInfo.AvgBuyPrice);
            var net = totalSell - totalBuy;

            result.Add(new EnergyRevenueDataPoint
            {
                Timestamp = $"{year}-{month:D2}",
                Buy = Math.Round(totalBuy, 2),
                Sell = Math.Round(totalSell, 2),
                Net = Math.Round(net, 2),
                Price = Math.Round(totalPrice, 2),
                BuyPriceUsed = monthPriceInfo.AvgBuyPrice,
                SellPriceUsed = monthPriceInfo.AvgSellPrice
            });
        }

        return result;
    }

    /// <summary>
    /// Calculate weighted average prices for a month based on daily data
    /// Each day's price is weighted by the energy consumed/produced that day
    /// </summary>
    private async Task<(double AvgSellPrice, double AvgBuyPrice)> CalculateMonthlyPriceInfoAsync(
        List<DeviceEnergyData> monthDailyData,
        DateTime monthStart,
        DateTime monthEnd,
        CancellationToken cancellationToken)
    {
        if (!monthDailyData.Any())
        {
            // No data - get prices for mid-month
            var midMonth = monthStart.AddDays(14);
            var prices = await GetPricesEffectiveAtAsync(midMonth, cancellationToken);
            return GetWeightedDailyAveragePrice(prices);
        }

        double totalWeightedSell = 0;
        double totalWeightedBuy = 0;
        double totalSellEnergy = 0;
        double totalBuyEnergy = 0;

        foreach (var dayData in monthDailyData)
        {
            var prices = await GetPricesEffectiveAtAsync(dayData.DeviceLocalTime, cancellationToken);
            var (sellPrice, buyPrice) = GetWeightedDailyAveragePrice(prices);

            var daySell = dayData.GridExportEnergyKWh ?? 0;
            var dayBuy = dayData.GridImportEnergyKWh ?? 0;

            totalWeightedSell += daySell * sellPrice;
            totalWeightedBuy += dayBuy * buyPrice;
            totalSellEnergy += daySell;
            totalBuyEnergy += dayBuy;
        }

        var avgSellPrice = totalSellEnergy > 0 ? totalWeightedSell / totalSellEnergy : 0;
        var avgBuyPrice = totalBuyEnergy > 0 ? totalWeightedBuy / totalBuyEnergy : 0;

        return (Math.Round(avgSellPrice, 4), Math.Round(avgBuyPrice, 4));
    }
}
