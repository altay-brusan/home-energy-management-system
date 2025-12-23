using Hems_Backend.Application.Dashboard.Commands.SetEnergyPrice;
using Hems_Backend.Application.Dashboard.Queries.GetEnergyPrices;
using Hems_Backend.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace Hems_Backend.Web.Endpoints;

/// <summary>
/// Energy Revenue API - Manages energy prices and revenue calculations
/// </summary>
public class EnergyRevenue : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapPost(SetEnergyPrice, "{deviceId:guid}/prices")
            .MapGet(GetEnergyPrices, "{deviceId:guid}/prices")
            .MapGet(GetEnergyRevenue, "{deviceId:guid}");
    }

    /// <summary>
    /// Set sell and buy prices for a device
    /// </summary>
    /// <remarks>
    /// Accepts up to 6 price entries per request.
    /// Each price entry must have sellPrice and buyPrice.
    /// Optionally specify effectiveFrom date; defaults to now.
    /// </remarks>
    [Authorize]
    public async Task<IResult> SetEnergyPrice(
        ISender sender,
        Guid deviceId,
        SetEnergyPriceRequest request)
    {
        var command = new SetEnergyPriceCommand
        {
            DeviceId = deviceId,
            Unit = request.Unit,
            Currency = request.Currency,
            Prices = request.Prices.Select(p => new PriceEntry
            {
                SellPrice = p.SellPrice,
                BuyPrice = p.BuyPrice,
                EffectiveFrom = p.EffectiveFrom,
                BeginHour = p.BeginHour,
                EndHour = p.EndHour
            }).ToList()
        };

        var result = await sender.Send(command);

        if (result.Success)
        {
            return Results.Ok(result);
        }

        return Results.BadRequest(new
        {
            success = false,
            message = result.ErrorMessage
        });
    }

    /// <summary>
    /// Get energy prices for a device
    /// </summary>
    [Authorize]
    public async Task<IResult> GetEnergyPrices(
        ISender sender,
        Guid deviceId,
        bool includeHistory = true)
    {
        var query = new GetEnergyPricesQuery
        {
            DeviceId = deviceId,
            IncludeHistory = includeHistory
        };

        var result = await sender.Send(query);

        if (!result.Success)
        {
            return Results.BadRequest(new
            {
                success = false,
                message = result.ErrorMessage
            });
        }

        // Return only price-related data
        return Results.Ok(new
        {
            success = true,
            deviceId = result.DeviceId,
            hasPrices = result.HasPrices,
            currentPrice = result.CurrentPrice,
            priceHistory = result.PriceHistory
        });
    }

    /// <summary>
    /// Get energy revenue for a device
    /// </summary>
    /// <remarks>
    /// Returns current price, price history, and revenue calculation.
    /// Revenue is calculated based on grid export/import energy and the prices
    /// that were effective during each time period.
    ///
    /// If no prices are set, returns an error prompting user to set prices first.
    /// </remarks>
    [Authorize]
    public async Task<IResult> GetEnergyRevenue(
        ISender sender,
        Guid deviceId,
        bool includeHistory = true,
        DateTimeOffset? revenueStartDate = null,
        DateTimeOffset? revenueEndDate = null)
    {
        var query = new GetEnergyPricesQuery
        {
            DeviceId = deviceId,
            IncludeHistory = includeHistory,
            RevenueStartDate = revenueStartDate,
            RevenueEndDate = revenueEndDate
        };

        var result = await sender.Send(query);

        if (!result.Success)
        {
            return Results.BadRequest(new
            {
                success = false,
                message = result.ErrorMessage
            });
        }

        return Results.Ok(result);
    }
}

// Request DTOs
public record SetEnergyPriceRequest
{
    /// <summary>
    /// Unit of energy measurement (e.g., "kWh")
    /// </summary>
    public string Unit { get; init; } = "kWh";

    /// <summary>
    /// Currency code (e.g., "EUR", "USD", "TRY")
    /// </summary>
    public string Currency { get; init; } = "EUR";

    /// <summary>
    /// List of price entries (up to 6)
    /// </summary>
    public List<PriceEntryRequest> Prices { get; init; } = new();
}

public record PriceEntryRequest
{
    /// <summary>
    /// Price at which energy is sold to the grid
    /// </summary>
    public decimal SellPrice { get; init; }

    /// <summary>
    /// Price at which energy is bought from the grid
    /// </summary>
    public decimal BuyPrice { get; init; }

    /// <summary>
    /// When this price becomes effective (optional, defaults to now)
    /// </summary>
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
