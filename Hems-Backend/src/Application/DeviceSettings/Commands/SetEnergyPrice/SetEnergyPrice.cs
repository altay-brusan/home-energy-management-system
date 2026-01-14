using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.DeviceSettings.Commands.SetEnergyPrice;

/// <summary>
/// Price entry for a specific time range
/// </summary>
public record PriceEntry
{
    public decimal SellPrice { get; init; }
    public decimal BuyPrice { get; init; }
    public int? BeginHour { get; init; }
    public int? EndHour { get; init; }
}

/// <summary>
/// Command to set energy prices for a device
/// </summary>
public record SetEnergyPriceCommand : IRequest<SetEnergyPriceResult>
{
    public Guid DeviceId { get; init; }
    public string Unit { get; init; } = "kWh";
    public string Currency { get; init; } = "EUR";
    public List<PriceEntry> Prices { get; init; } = new();
}

/// <summary>
/// Result of setting energy prices
/// </summary>
public record SetEnergyPriceResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int PricesCreated { get; init; }
    public List<Guid> PriceIds { get; init; } = new();
}

public class SetEnergyPriceCommandHandler : IRequestHandler<SetEnergyPriceCommand, SetEnergyPriceResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SetEnergyPriceCommandHandler> _logger;
    private readonly IDemoDataService _demoDataService;
    private readonly IDemoSimulationDataService _demoSimulationService;

    private const int MaxPricesPerRequest = 24; // Allow up to 24 hourly prices

    public SetEnergyPriceCommandHandler(
        IApplicationDbContext context,
        IUser user,
        TimeProvider timeProvider,
        ILogger<SetEnergyPriceCommandHandler> logger,
        IDemoDataService demoDataService,
        IDemoSimulationDataService demoSimulationService)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
        _logger = logger;
        _demoDataService = demoDataService;
        _demoSimulationService = demoSimulationService;
    }

    public async Task<SetEnergyPriceResult> Handle(SetEnergyPriceCommand request, CancellationToken cancellationToken)
    {
        // Validate device exists
        var device = await _context.Devices
            .Where(d => d.Id == request.DeviceId && !d.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (device == null)
        {
            return new SetEnergyPriceResult
            {
                Success = false,
                ErrorMessage = "Device not found"
            };
        }

        // Validate price entries
        if (request.Prices.Count == 0)
        {
            return new SetEnergyPriceResult
            {
                Success = false,
                ErrorMessage = "At least one price entry is required"
            };
        }

        if (request.Prices.Count > MaxPricesPerRequest)
        {
            return new SetEnergyPriceResult
            {
                Success = false,
                ErrorMessage = $"Maximum {MaxPricesPerRequest} price entries allowed per request"
            };
        }

        // Validate hour ranges
        foreach (var price in request.Prices)
        {
            if (price.BeginHour.HasValue && (price.BeginHour < 0 || price.BeginHour > 23))
            {
                return new SetEnergyPriceResult
                {
                    Success = false,
                    ErrorMessage = "BeginHour must be between 0 and 23"
                };
            }

            if (price.EndHour.HasValue && (price.EndHour < 0 || price.EndHour > 24))
            {
                return new SetEnergyPriceResult
                {
                    Success = false,
                    ErrorMessage = "EndHour must be between 0 and 24"
                };
            }

            if (price.BeginHour.HasValue && price.EndHour.HasValue && price.BeginHour >= price.EndHour)
            {
                return new SetEnergyPriceResult
                {
                    Success = false,
                    ErrorMessage = "BeginHour must be less than EndHour"
                };
            }
        }

        // Check if this is a demo device - store in-memory instead of database
        var isDemoDevice = await _demoDataService.IsDemoDeviceAsync(request.DeviceId, cancellationToken);
        if (isDemoDevice)
        {
            return HandleDemoDevicePrices(request);
        }

        // For real devices: persist to database
        // Deactivate existing prices for this device
        var existingPrices = await _context.EnergyPriceHistory
            .Where(p => p.DeviceId == request.DeviceId && p.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var existingPrice in existingPrices)
        {
            existingPrice.IsActive = false;
        }

        // Create new price entries
        var createdIds = new List<Guid>();
        foreach (var priceEntry in request.Prices)
        {
            var energyPrice = new EnergyPriceHistory
            {
                DeviceId = request.DeviceId,
                Unit = request.Unit,
                Currency = request.Currency,
                SellPrice = priceEntry.SellPrice,
                BuyPrice = priceEntry.BuyPrice,
                BeginHour = priceEntry.BeginHour,
                EndHour = priceEntry.EndHour,
                IsActive = true
            };

            _context.EnergyPriceHistory.Add(energyPrice);
            createdIds.Add(energyPrice.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Set {Count} energy prices for device {DeviceId} by user {UserId}",
            createdIds.Count, request.DeviceId, _user.Id);

        return new SetEnergyPriceResult
        {
            Success = true,
            PricesCreated = createdIds.Count,
            PriceIds = createdIds
        };
    }

    /// <summary>
    /// Handle price setting for demo devices - store in-memory
    /// </summary>
    private SetEnergyPriceResult HandleDemoDevicePrices(SetEnergyPriceCommand request)
    {
        var demoPrices = request.Prices.Select(p => new DemoPriceEntry
        {
            SellPrice = p.SellPrice,
            BuyPrice = p.BuyPrice,
            BeginHour = p.BeginHour,
            EndHour = p.EndHour
        }).ToList();

        _demoSimulationService.SetDemoPrices(request.DeviceId, demoPrices, request.Unit, request.Currency);

        // Generate fake IDs for response consistency
        var fakeIds = request.Prices.Select(_ => Guid.NewGuid()).ToList();

        _logger.LogInformation(
            "Set {Count} energy prices for DEMO device {DeviceId} (in-memory)",
            demoPrices.Count, request.DeviceId);

        return new SetEnergyPriceResult
        {
            Success = true,
            PricesCreated = demoPrices.Count,
            PriceIds = fakeIds
        };
    }
}
