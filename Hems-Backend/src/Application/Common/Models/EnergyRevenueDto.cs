namespace Hems_Backend.Application.Common.Models;

/// <summary>
/// Energy revenue response showing historical buy/sell energy and calculated revenue
/// </summary>
public record EnergyRevenueDto
{
    /// <summary>
    /// The period type: "monthly", "yearly", or "custom"
    /// </summary>
    public string Period { get; init; } = string.Empty;

    /// <summary>
    /// Current price per kWh for buying energy from grid
    /// </summary>
    public double BuyPricePerKWh { get; init; }

    /// <summary>
    /// Current price per kWh for selling energy to grid
    /// </summary>
    public double SellPricePerKWh { get; init; }

    /// <summary>
    /// Currency code (e.g., "EUR", "USD", "TRY")
    /// </summary>
    public string Currency { get; init; } = "EUR";

    /// <summary>
    /// List of revenue data points for the specified period
    /// </summary>
    public List<EnergyRevenueDataPoint> Data { get; init; } = new();

    /// <summary>
    /// Total aggregated values for the entire period
    /// </summary>
    public EnergyRevenueTotal Total { get; init; } = new();
}

/// <summary>
/// Individual data point for energy revenue
/// </summary>
public record EnergyRevenueDataPoint
{
    /// <summary>
    /// Timestamp label (e.g., "2024-12-01" for daily, "2024-12" for monthly)
    /// </summary>
    public string Timestamp { get; init; } = string.Empty;

    /// <summary>
    /// Energy purchased from grid in kWh
    /// </summary>
    public double Buy { get; init; }

    /// <summary>
    /// Energy sold to grid in kWh
    /// </summary>
    public double Sell { get; init; }

    /// <summary>
    /// Net energy (Sell - Buy) in kWh
    /// Positive = net export, Negative = net import
    /// </summary>
    public double Net { get; init; }

    /// <summary>
    /// Calculated revenue/cost
    /// Formula: (Sell × SellPrice) - (Buy × BuyPrice)
    /// Positive = revenue/profit, Negative = cost/loss
    /// </summary>
    public double Price { get; init; }

    /// <summary>
    /// Buy price per kWh used for this data point's calculation
    /// This reflects the price that was effective at the time of this data point
    /// </summary>
    public double BuyPriceUsed { get; init; }

    /// <summary>
    /// Sell price per kWh used for this data point's calculation
    /// This reflects the price that was effective at the time of this data point
    /// </summary>
    public double SellPriceUsed { get; init; }
}

/// <summary>
/// Total aggregated values for energy revenue
/// </summary>
public record EnergyRevenueTotal
{
    /// <summary>
    /// Total cost of energy purchased from grid in EUR
    /// </summary>
    public double BuyPrice { get; init; }

    /// <summary>
    /// Total revenue from energy sold to grid in EUR
    /// </summary>
    public double SellPrice { get; init; }

    /// <summary>
    /// Net revenue/cost in EUR (SellPrice - BuyPrice)
    /// Positive = net revenue/profit, Negative = net cost/loss
    /// </summary>
    public double NetPrice { get; init; }
}
