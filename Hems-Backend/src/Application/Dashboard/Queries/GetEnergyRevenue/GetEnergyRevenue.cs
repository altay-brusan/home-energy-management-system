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

        // Get date range based on period
        DateTime startDate;
        DateTime endDate;

        switch (period)
        {
            case "monthly":
                var today = DateTime.UtcNow.Date;
                startDate = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                endDate = startDate.AddMonths(1);
                break;
            case "yearly":
                var currentYear = DateTime.UtcNow.Date;
                startDate = new DateTime(currentYear.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                endDate = startDate.AddYears(1);
                break;
            case "custom":
                startDate = DateTime.SpecifyKind(request.StartDate!.Value.Date, DateTimeKind.Utc);
                endDate = DateTime.SpecifyKind(request.EndDate!.Value.Date.AddDays(1), DateTimeKind.Utc);
                break;
            default:
                throw new ArgumentException($"Invalid period: {period}");
        }

        // Query energy data for the period
        var energyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       x.DataDateTime >= startDate &&
                       x.DataDateTime < endDate &&
                       x.DataType == DataTypes.DailyCumulativeEnergy)
            .ToListAsync(cancellationToken);

        // Calculate totals
        var totalBuy = energyData.Sum(x => x.GridImportEnergyKWh ?? 0);
        var totalSell = energyData.Sum(x => x.GridExportEnergyKWh ?? 0);
        var netEnergy = totalSell - totalBuy;

        // Calculate revenue: (Sell × SellPrice) - (Buy × BuyPrice)
        var revenue = (totalSell * SellPricePerKWh) - (totalBuy * BuyPricePerKWh);

        return new EnergyRevenueDto
        {
            Buy = Math.Round(totalBuy, 2),
            Sell = Math.Round(totalSell, 2),
            Net = Math.Round(netEnergy, 2),
            Price = Math.Round(revenue, 2),
            BuyPricePerKWh = BuyPricePerKWh,
            SellPricePerKWh = SellPricePerKWh
        };
    }
}
