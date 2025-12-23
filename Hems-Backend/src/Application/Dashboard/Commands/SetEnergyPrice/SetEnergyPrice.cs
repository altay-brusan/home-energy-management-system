using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.Dashboard.Commands.SetEnergyPrice;

/// <summary>
/// A single price entry with sell and buy prices
/// </summary>
public record PriceEntry
{
    public decimal SellPrice { get; init; }
    public decimal BuyPrice { get; init; }
    public DateTimeOffset? EffectiveFrom { get; init; }

    /// <summary>
    /// Start hour of the day when this price is valid (0-23).
    /// Null means the price starts from 00:00.
    /// </summary>
    public int? BeginHour { get; init; }

    /// <summary>
    /// End hour of the day when this price is valid (0-23).
    /// Null means the price is valid until 23:59.
    /// If both BeginHour and EndHour are null, the price applies all day.
    /// </summary>
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

    private const int MaxPricesPerRequest = 6;

    public SetEnergyPriceCommandHandler(
        IApplicationDbContext context,
        IUser user,
        TimeProvider timeProvider,
        ILogger<SetEnergyPriceCommandHandler> logger)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<SetEnergyPriceResult> Handle(SetEnergyPriceCommand request, CancellationToken cancellationToken)
    {
        // Validate device exists
        var deviceExists = await _context.Devices
            .AnyAsync(d => d.Id == request.DeviceId && !d.IsDeleted, cancellationToken);

        if (!deviceExists)
        {
            return new SetEnergyPriceResult
            {
                Success = false,
                ErrorMessage = "Device not found"
            };
        }

        // Validate price count
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

        // Validate prices
        foreach (var price in request.Prices)
        {
            if (price.SellPrice < 0)
            {
                return new SetEnergyPriceResult
                {
                    Success = false,
                    ErrorMessage = "Sell price cannot be negative"
                };
            }

            if (price.BuyPrice < 0)
            {
                return new SetEnergyPriceResult
                {
                    Success = false,
                    ErrorMessage = "Buy price cannot be negative"
                };
            }

            // Validate hour range (0-23)
            if (price.BeginHour.HasValue && (price.BeginHour < 0 || price.BeginHour > 23))
            {
                return new SetEnergyPriceResult
                {
                    Success = false,
                    ErrorMessage = "BeginHour must be between 0 and 23"
                };
            }

            if (price.EndHour.HasValue && (price.EndHour < 0 || price.EndHour > 23))
            {
                return new SetEnergyPriceResult
                {
                    Success = false,
                    ErrorMessage = "EndHour must be between 0 and 23"
                };
            }

            // If BeginHour is set, EndHour should be set and vice versa (or both null)
            if (price.BeginHour.HasValue != price.EndHour.HasValue)
            {
                return new SetEnergyPriceResult
                {
                    Success = false,
                    ErrorMessage = "Both BeginHour and EndHour must be set, or both must be null for all-day pricing"
                };
            }

            // Validate BeginHour < EndHour (or handle overnight case)
            if (price.BeginHour.HasValue && price.EndHour.HasValue && price.BeginHour >= price.EndHour)
            {
                return new SetEnergyPriceResult
                {
                    Success = false,
                    ErrorMessage = "BeginHour must be less than EndHour"
                };
            }
        }

        var utcNow = _timeProvider.GetUtcNow();
        var createdPriceIds = new List<Guid>();

        // Deactivate existing active prices for this device
        var existingActivePrices = await _context.DevicePrices
            .Where(dp => dp.DeviceId == request.DeviceId && dp.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var existingPrice in existingActivePrices)
        {
            existingPrice.IsActive = false;
            existingPrice.EffectiveTo = utcNow;
            existingPrice.LastModified = utcNow;
            existingPrice.LastModifiedBy = _user.Id;
        }

        // Create new price entries
        foreach (var priceEntry in request.Prices)
        {
            var effectiveFrom = priceEntry.EffectiveFrom ?? utcNow;

            var devicePrice = new DevicePrice
            {
                DeviceId = request.DeviceId,
                Unit = request.Unit,
                Currency = request.Currency,
                SellPrice = priceEntry.SellPrice,
                BuyPrice = priceEntry.BuyPrice,
                EffectiveFrom = effectiveFrom,
                BeginHour = priceEntry.BeginHour,
                EndHour = priceEntry.EndHour,
                IsActive = true,
                Created = utcNow,
                CreatedBy = _user.Id,
                LastModified = utcNow,
                LastModifiedBy = _user.Id
            };

            _context.DevicePrices.Add(devicePrice);
            createdPriceIds.Add(devicePrice.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created {Count} energy price entries for device {DeviceId}",
            createdPriceIds.Count, request.DeviceId);

        return new SetEnergyPriceResult
        {
            Success = true,
            PricesCreated = createdPriceIds.Count,
            PriceIds = createdPriceIds
        };
    }
}
