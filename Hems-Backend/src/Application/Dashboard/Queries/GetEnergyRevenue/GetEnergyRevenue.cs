using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Dashboard.Queries.GetEnergyRevenue;

public record GetEnergyRevenueQuery : IRequest<EnergyRevenueDto>
{
    public Guid DeviceId { get; init; }
    public string Period { get; init; } = string.Empty; // "monthly", "yearly", "custom"
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
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

        // Validate period
        var period = request.Period.ToLower();
        if (period != "monthly" && period != "yearly" && period != "custom")
        {
            throw new ArgumentException("Period must be 'monthly', 'yearly', or 'custom'");
        }

        // Validate custom range
        if (period == "custom" && (!request.StartDate.HasValue || !request.EndDate.HasValue))
        {
            throw new ArgumentException("StartDate and EndDate are required for custom period");
        }

        List<EnergyRevenueDataPoint> dataPoints;

        switch (period)
        {
            case "monthly":
                dataPoints = await GetMonthlyDataAsync(thingsBoardDeviceId.Value, cancellationToken);
                break;
            case "yearly":
                dataPoints = await GetYearlyDataAsync(thingsBoardDeviceId.Value, cancellationToken);
                break;
            case "custom":
                dataPoints = await GetCustomRangeDataAsync(thingsBoardDeviceId.Value, request.StartDate!.Value, request.EndDate!.Value, cancellationToken);
                break;
            default:
                throw new ArgumentException($"Invalid period: {period}");
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
    /// Get daily revenue data for the current month
    /// </summary>
    private async Task<List<EnergyRevenueDataPoint>> GetMonthlyDataAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
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
            .OrderBy(d => d.DataDateTime);

        return latestDailyData.Select(d =>
        {
            var buy = d.GridImportEnergyKWh ?? 0;
            var sell = d.GridExportEnergyKWh ?? 0;
            var net = sell - buy;
            var price = (sell * SellPricePerKWh) - (buy * BuyPricePerKWh);

            return new EnergyRevenueDataPoint
            {
                Timestamp = d.DataDateTime.ToString("yyyy-MM-dd"),
                Buy = Math.Round(buy, 2),
                Sell = Math.Round(sell, 2),
                Net = Math.Round(net, 2),
                Price = Math.Round(price, 2)
            };
        }).ToList();
    }

    /// <summary>
    /// Get monthly aggregated revenue data for the current year
    /// </summary>
    private async Task<List<EnergyRevenueDataPoint>> GetYearlyDataAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var yearStart = new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
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
            .Select(g =>
            {
                var buy = g.Sum(d => d.GridImportEnergyKWh ?? 0);
                var sell = g.Sum(d => d.GridExportEnergyKWh ?? 0);
                var net = sell - buy;
                var price = (sell * SellPricePerKWh) - (buy * BuyPricePerKWh);

                return new EnergyRevenueDataPoint
                {
                    Timestamp = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Buy = Math.Round(buy, 2),
                    Sell = Math.Round(sell, 2),
                    Net = Math.Round(net, 2),
                    Price = Math.Round(price, 2)
                };
            })
            .OrderBy(d => d.Timestamp)
            .ToList();

        return monthlyData;
    }

    /// <summary>
    /// Get daily revenue data for a custom date range
    /// </summary>
    private async Task<List<EnergyRevenueDataPoint>> GetCustomRangeDataAsync(
        Guid thingsBoardDeviceId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        // Ensure dates are in UTC and normalized to start of day
        var rangeStart = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc);
        var rangeEnd = DateTime.SpecifyKind(endDate.Date.AddDays(1), DateTimeKind.Utc); // Include the end date

        var dailyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DataDateTime >= rangeStart &&
                       x.DataDateTime < rangeEnd &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .OrderBy(x => x.DataDateTime)
            .ToListAsync(cancellationToken);

        // Group by date and take the most recent entry for each day
        var latestDailyData = dailyData
            .GroupBy(d => d.DataDateTime.Date)
            .Select(g => g.OrderByDescending(d => d.DataDateTime).First())
            .OrderBy(d => d.DataDateTime);

        return latestDailyData.Select(d =>
        {
            var buy = d.GridImportEnergyKWh ?? 0;
            var sell = d.GridExportEnergyKWh ?? 0;
            var net = sell - buy;
            var price = (sell * SellPricePerKWh) - (buy * BuyPricePerKWh);

            return new EnergyRevenueDataPoint
            {
                Timestamp = d.DataDateTime.ToString("yyyy-MM-dd"),
                Buy = Math.Round(buy, 2),
                Sell = Math.Round(sell, 2),
                Net = Math.Round(net, 2),
                Price = Math.Round(price, 2)
            };
        }).ToList();
    }
}
