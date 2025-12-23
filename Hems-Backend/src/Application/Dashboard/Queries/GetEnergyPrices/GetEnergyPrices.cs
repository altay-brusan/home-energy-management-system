using Hems_Backend.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.Dashboard.Queries.GetEnergyPrices;

/// <summary>
/// DTO for a device price entry
/// </summary>
public record EnergyPriceDto
{
    public Guid Id { get; init; }
    public Guid DeviceId { get; init; }
    public string Unit { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public decimal SellPrice { get; init; }
    public decimal BuyPrice { get; init; }
    public DateTimeOffset EffectiveFrom { get; init; }
    public DateTimeOffset? EffectiveTo { get; init; }
    public int? BeginHour { get; init; }
    public int? EndHour { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset Created { get; init; }
    public string? CreatedBy { get; init; }
    public DateTimeOffset LastModified { get; init; }
    public string? LastModifiedBy { get; init; }
}

/// <summary>
/// Revenue calculation for a specific price period
/// </summary>
public record RevenuePeriod
{
    public DateTimeOffset PeriodStart { get; init; }
    public DateTimeOffset PeriodEnd { get; init; }
    public decimal SellPrice { get; init; }
    public decimal BuyPrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;

    // Energy values (kWh)
    public double GridExportEnergyKWh { get; init; }
    public double GridImportEnergyKWh { get; init; }

    // Revenue calculations
    public decimal SellRevenue { get; init; }
    public decimal BuyCost { get; init; }
    public decimal NetRevenue { get; init; }
}

/// <summary>
/// Revenue summary
/// </summary>
public record RevenueSummary
{
    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset EndDate { get; init; }
    public double TotalGridExportEnergyKWh { get; init; }
    public double TotalGridImportEnergyKWh { get; init; }
    public decimal TotalSellRevenue { get; init; }
    public decimal TotalBuyCost { get; init; }
    public decimal TotalNetRevenue { get; init; }
    public string Currency { get; init; } = string.Empty;
    public List<RevenuePeriod> PeriodBreakdown { get; init; } = new();
}

/// <summary>
/// Result for energy prices query
/// </summary>
public record GetEnergyPricesResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid DeviceId { get; init; }
    public bool HasPrices { get; init; }
    public EnergyPriceDto? CurrentPrice { get; init; }
    public List<EnergyPriceDto> PriceHistory { get; init; } = new();
    public RevenueSummary? Revenue { get; init; }
}

/// <summary>
/// Query to get energy prices and calculate revenue
/// </summary>
public record GetEnergyPricesQuery : IRequest<GetEnergyPricesResult>
{
    public Guid DeviceId { get; init; }
    public bool IncludeHistory { get; init; } = true;

    /// <summary>
    /// Start date for revenue calculation. If not specified, calculates from the first price effective date.
    /// </summary>
    public DateTimeOffset? RevenueStartDate { get; init; }

    /// <summary>
    /// End date for revenue calculation. If not specified, calculates up to now.
    /// </summary>
    public DateTimeOffset? RevenueEndDate { get; init; }
}

public class GetEnergyPricesQueryHandler : IRequestHandler<GetEnergyPricesQuery, GetEnergyPricesResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetEnergyPricesQueryHandler> _logger;

    public GetEnergyPricesQueryHandler(
        IApplicationDbContext context,
        ILogger<GetEnergyPricesQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GetEnergyPricesResult> Handle(GetEnergyPricesQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        // Validate device exists and get ThingsBoardDeviceId
        var device = await _context.Devices
            .Where(d => d.Id == request.DeviceId && !d.IsDeleted)
            .Select(d => new { d.Id, d.ThingsBoardDeviceId })
            .FirstOrDefaultAsync(cancellationToken);

        if (device == null)
        {
            return new GetEnergyPricesResult
            {
                Success = false,
                ErrorMessage = "Device not found",
                DeviceId = request.DeviceId
            };
        }

        // Get all prices for this device
        var allPrices = await _context.DevicePrices
            .Where(dp => dp.DeviceId == request.DeviceId)
            .OrderByDescending(dp => dp.EffectiveFrom)
            .ToListAsync(cancellationToken);

        if (!allPrices.Any())
        {
            return new GetEnergyPricesResult
            {
                Success = false,
                ErrorMessage = "No price data found for this device. Please set sell and buy prices before calculating revenue.",
                DeviceId = request.DeviceId,
                HasPrices = false
            };
        }

        // Get current active price
        var currentPrice = allPrices
            .Where(dp => dp.IsActive &&
                         dp.EffectiveFrom <= now &&
                         (dp.EffectiveTo == null || dp.EffectiveTo > now))
            .OrderByDescending(dp => dp.EffectiveFrom)
            .FirstOrDefault();

        var currentPriceDto = currentPrice != null ? MapToDto(currentPrice) : null;

        // Get price history
        var priceHistory = request.IncludeHistory
            ? allPrices.Select(MapToDto).ToList()
            : new List<EnergyPriceDto>();

        // Calculate revenue
        var revenueStartDate = request.RevenueStartDate ?? allPrices.Min(p => p.EffectiveFrom);
        var revenueEndDate = request.RevenueEndDate ?? now;

        var revenue = await CalculateRevenueAsync(
            device.ThingsBoardDeviceId,
            request.DeviceId,
            allPrices,
            revenueStartDate,
            revenueEndDate,
            cancellationToken);

        return new GetEnergyPricesResult
        {
            Success = true,
            DeviceId = request.DeviceId,
            HasPrices = true,
            CurrentPrice = currentPriceDto,
            PriceHistory = priceHistory,
            Revenue = revenue
        };
    }

    private static EnergyPriceDto MapToDto(Domain.Entities.DevicePrice dp)
    {
        return new EnergyPriceDto
        {
            Id = dp.Id,
            DeviceId = dp.DeviceId,
            Unit = dp.Unit,
            Currency = dp.Currency,
            SellPrice = dp.SellPrice,
            BuyPrice = dp.BuyPrice,
            EffectiveFrom = dp.EffectiveFrom,
            EffectiveTo = dp.EffectiveTo,
            BeginHour = dp.BeginHour,
            EndHour = dp.EndHour,
            IsActive = dp.IsActive,
            Created = dp.Created,
            CreatedBy = dp.CreatedBy,
            LastModified = dp.LastModified,
            LastModifiedBy = dp.LastModifiedBy
        };
    }

    private async Task<RevenueSummary> CalculateRevenueAsync(
        Guid thingsBoardDeviceId,
        Guid deviceId,
        List<Domain.Entities.DevicePrice> prices,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken)
    {
        // Get energy data for the period
        var energyData = await _context.DeviceEnergyData
            .Where(ed => ed.ThingsBoardDeviceId == thingsBoardDeviceId &&
                         ed.DataDateTime >= startDate.UtcDateTime &&
                         ed.DataDateTime <= endDate.UtcDateTime &&
                         ed.DataType == "DailyCumulativeEnergyData")
            .OrderBy(ed => ed.DataDateTime)
            .ToListAsync(cancellationToken);

        if (!energyData.Any())
        {
            return new RevenueSummary
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalGridExportEnergyKWh = 0,
                TotalGridImportEnergyKWh = 0,
                TotalSellRevenue = 0,
                TotalBuyCost = 0,
                TotalNetRevenue = 0,
                Currency = prices.First().Currency,
                PeriodBreakdown = new List<RevenuePeriod>()
            };
        }

        // Build price periods
        var pricePeriods = BuildPricePeriods(prices, startDate, endDate);

        // Calculate revenue for each period
        var periodBreakdowns = new List<RevenuePeriod>();
        double totalExport = 0;
        double totalImport = 0;
        decimal totalSellRevenue = 0;
        decimal totalBuyCost = 0;

        foreach (var pricePeriod in pricePeriods)
        {
            // Find energy data within this price period
            var periodEnergyData = energyData
                .Where(ed => ed.DataDateTime >= pricePeriod.Start.UtcDateTime &&
                             ed.DataDateTime < pricePeriod.End.UtcDateTime)
                .ToList();

            if (!periodEnergyData.Any())
                continue;

            // Sum energy values for this period
            double periodExport = periodEnergyData
                .Where(ed => ed.GridExportEnergyKWh.HasValue)
                .Sum(ed => ed.GridExportEnergyKWh ?? 0);

            double periodImport = periodEnergyData
                .Where(ed => ed.GridImportEnergyKWh.HasValue)
                .Sum(ed => ed.GridImportEnergyKWh ?? 0);

            // Calculate revenue
            decimal sellRevenue = (decimal)periodExport * pricePeriod.SellPrice;
            decimal buyCost = (decimal)periodImport * pricePeriod.BuyPrice;
            decimal netRevenue = sellRevenue - buyCost;

            periodBreakdowns.Add(new RevenuePeriod
            {
                PeriodStart = pricePeriod.Start,
                PeriodEnd = pricePeriod.End,
                SellPrice = pricePeriod.SellPrice,
                BuyPrice = pricePeriod.BuyPrice,
                Currency = pricePeriod.Currency,
                Unit = pricePeriod.Unit,
                GridExportEnergyKWh = periodExport,
                GridImportEnergyKWh = periodImport,
                SellRevenue = sellRevenue,
                BuyCost = buyCost,
                NetRevenue = netRevenue
            });

            totalExport += periodExport;
            totalImport += periodImport;
            totalSellRevenue += sellRevenue;
            totalBuyCost += buyCost;
        }

        _logger.LogInformation(
            "Calculated revenue for device {DeviceId}: Export={ExportKWh}kWh, Import={ImportKWh}kWh, NetRevenue={NetRevenue}",
            deviceId, totalExport, totalImport, totalSellRevenue - totalBuyCost);

        return new RevenueSummary
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalGridExportEnergyKWh = totalExport,
            TotalGridImportEnergyKWh = totalImport,
            TotalSellRevenue = totalSellRevenue,
            TotalBuyCost = totalBuyCost,
            TotalNetRevenue = totalSellRevenue - totalBuyCost,
            Currency = prices.First().Currency,
            PeriodBreakdown = periodBreakdowns
        };
    }

    /// <summary>
    /// Build effective price periods, handling overlaps and gaps
    /// </summary>
    private static List<PricePeriodInfo> BuildPricePeriods(
        List<Domain.Entities.DevicePrice> prices,
        DateTimeOffset queryStart,
        DateTimeOffset queryEnd)
    {
        var periods = new List<PricePeriodInfo>();

        // Order by EffectiveFrom ascending for period building
        var orderedPrices = prices.OrderBy(p => p.EffectiveFrom).ToList();

        for (int i = 0; i < orderedPrices.Count; i++)
        {
            var price = orderedPrices[i];

            // Skip prices that ended before query start or started after query end
            if (price.EffectiveTo.HasValue && price.EffectiveTo.Value < queryStart)
                continue;
            if (price.EffectiveFrom > queryEnd)
                continue;

            // Determine the start of this price's effective period
            var periodStart = price.EffectiveFrom < queryStart ? queryStart : price.EffectiveFrom;

            // Determine the end of this price's effective period
            DateTimeOffset periodEnd;
            if (price.EffectiveTo.HasValue)
            {
                periodEnd = price.EffectiveTo.Value > queryEnd ? queryEnd : price.EffectiveTo.Value;
            }
            else if (i + 1 < orderedPrices.Count)
            {
                // End at the next price's effective date
                periodEnd = orderedPrices[i + 1].EffectiveFrom > queryEnd ? queryEnd : orderedPrices[i + 1].EffectiveFrom;
            }
            else
            {
                // This is the last price, end at query end
                periodEnd = queryEnd;
            }

            if (periodStart < periodEnd)
            {
                periods.Add(new PricePeriodInfo
                {
                    Start = periodStart,
                    End = periodEnd,
                    SellPrice = price.SellPrice,
                    BuyPrice = price.BuyPrice,
                    Currency = price.Currency,
                    Unit = price.Unit
                });
            }
        }

        return periods;
    }

    private record PricePeriodInfo
    {
        public DateTimeOffset Start { get; init; }
        public DateTimeOffset End { get; init; }
        public decimal SellPrice { get; init; }
        public decimal BuyPrice { get; init; }
        public string Currency { get; init; } = string.Empty;
        public string Unit { get; init; } = string.Empty;
    }
}
