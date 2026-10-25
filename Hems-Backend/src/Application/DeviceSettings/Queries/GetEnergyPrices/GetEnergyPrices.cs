using Hems_Backend.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceSettings.Queries.GetEnergyPrices;

/// <summary>
/// DTO for an energy price entry
/// </summary>
public record EnergyPriceDto
{
    public Guid Id { get; init; }
    public decimal SellPrice { get; init; }
    public decimal BuyPrice { get; init; }
    public int? BeginHour { get; init; }
    public int? EndHour { get; init; }
    public DateTimeOffset Created { get; init; }
}

/// <summary>
/// Result for energy prices query
/// </summary>
public record GetEnergyPricesResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid DeviceId { get; init; }
    public string Unit { get; init; } = "kWh";
    public string Currency { get; init; } = "EUR";
    public List<EnergyPriceDto> Prices { get; init; } = new();
}

/// <summary>
/// Query to get active energy prices for a device
/// </summary>
public record GetEnergyPricesQuery : IRequest<GetEnergyPricesResult>
{
    public Guid DeviceId { get; init; }
}

public class GetEnergyPricesQueryHandler : IRequestHandler<GetEnergyPricesQuery, GetEnergyPricesResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IDemoDataService _demoDataService;
    private readonly IDemoSimulationDataService _demoSimulationService;

    public GetEnergyPricesQueryHandler(
        IApplicationDbContext context,
        IDemoDataService demoDataService,
        IDemoSimulationDataService demoSimulationService)
    {
        _context = context;
        _demoDataService = demoDataService;
        _demoSimulationService = demoSimulationService;
    }

    public async Task<GetEnergyPricesResult> Handle(GetEnergyPricesQuery request, CancellationToken cancellationToken)
    {
        // Validate device exists
        var device = await _context.Devices
            .Where(d => d.Id == request.DeviceId && !d.IsDeleted)
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

        // Check if demo device - return from in-memory storage or defaults
        var isDemoDevice = await _demoDataService.IsDemoDeviceAsync(request.DeviceId, cancellationToken);
        if (isDemoDevice)
        {
            return GetDemoPrices(request.DeviceId);
        }

        // Get active prices for this device
        var prices = await _context.EnergyPriceHistory
            .Where(p => p.DeviceId == request.DeviceId && p.IsActive)
            .OrderBy(p => p.BeginHour)
            .Select(p => new EnergyPriceDto
            {
                Id = p.Id,
                SellPrice = p.SellPrice,
                BuyPrice = p.BuyPrice,
                BeginHour = p.BeginHour,
                EndHour = p.EndHour,
                Created = p.Created
            })
            .ToListAsync(cancellationToken);

        // Get unit and currency from first price (they should all be the same)
        var firstPrice = await _context.EnergyPriceHistory
            .Where(p => p.DeviceId == request.DeviceId && p.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        return new GetEnergyPricesResult
        {
            Success = true,
            DeviceId = request.DeviceId,
            Unit = firstPrice?.Unit ?? "kWh",
            Currency = firstPrice?.Currency ?? "EUR",
            Prices = prices
        };
    }

    /// <summary>
    /// Gets prices for demo device from in-memory storage or returns defaults.
    /// If user has set custom prices, those are returned.
    /// Otherwise, returns default Netherlands market prices.
    /// </summary>
    private GetEnergyPricesResult GetDemoPrices(Guid deviceId)
    {
        var priceSettings = _demoSimulationService.GetDemoPrices(deviceId);

        var prices = priceSettings.Prices.Select(p => new EnergyPriceDto
        {
            Id = Guid.NewGuid(),
            SellPrice = p.SellPrice,
            BuyPrice = p.BuyPrice,
            BeginHour = p.BeginHour,
            EndHour = p.EndHour,
            Created = priceSettings.Created
        }).ToList();

        return new GetEnergyPricesResult
        {
            Success = true,
            DeviceId = deviceId,
            Unit = priceSettings.Unit,
            Currency = priceSettings.Currency,
            Prices = prices
        };
    }
}
