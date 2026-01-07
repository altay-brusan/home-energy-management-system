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

    public GetEnergyPricesQueryHandler(
        IApplicationDbContext context,
        IDemoDataService demoDataService)
    {
        _context = context;
        _demoDataService = demoDataService;
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

        // Check if demo device - return simulated Netherlands market prices
        var isDemoDevice = await _demoDataService.IsDemoDeviceAsync(request.DeviceId, cancellationToken);
        if (isDemoDevice)
        {
            return GetSimulatedNetherlandsPrices(request.DeviceId);
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
    /// Returns simulated Netherlands electricity market prices (Time-of-Use pricing)
    /// Based on typical Dutch energy market rates (2024)
    /// </summary>
    private static GetEnergyPricesResult GetSimulatedNetherlandsPrices(Guid deviceId)
    {
        var prices = new List<EnergyPriceDto>
        {
            new EnergyPriceDto
            {
                Id = Guid.NewGuid(),
                SellPrice = 0.07m,  // Off-peak export
                BuyPrice = 0.22m,   // Off-peak import
                BeginHour = 0,
                EndHour = 6,
                Created = DateTimeOffset.UtcNow.AddDays(-30)
            },
            new EnergyPriceDto
            {
                Id = Guid.NewGuid(),
                SellPrice = 0.11m,  // Morning peak export
                BuyPrice = 0.35m,   // Morning peak import
                BeginHour = 6,
                EndHour = 9,
                Created = DateTimeOffset.UtcNow.AddDays(-30)
            },
            new EnergyPriceDto
            {
                Id = Guid.NewGuid(),
                SellPrice = 0.09m,  // Day standard export
                BuyPrice = 0.28m,   // Day standard import
                BeginHour = 9,
                EndHour = 17,
                Created = DateTimeOffset.UtcNow.AddDays(-30)
            },
            new EnergyPriceDto
            {
                Id = Guid.NewGuid(),
                SellPrice = 0.13m,  // Evening peak export
                BuyPrice = 0.42m,   // Evening peak import (highest)
                BeginHour = 17,
                EndHour = 21,
                Created = DateTimeOffset.UtcNow.AddDays(-30)
            },
            new EnergyPriceDto
            {
                Id = Guid.NewGuid(),
                SellPrice = 0.08m,  // Late evening export
                BuyPrice = 0.25m,   // Late evening import
                BeginHour = 21,
                EndHour = 24,
                Created = DateTimeOffset.UtcNow.AddDays(-30)
            }
        };

        return new GetEnergyPricesResult
        {
            Success = true,
            DeviceId = deviceId,
            Unit = "kWh",
            Currency = "EUR",
            Prices = prices
        };
    }
}
