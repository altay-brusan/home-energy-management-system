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
    /// Price per kWh for buying energy from grid in EUR
    /// </summary>
    public double BuyPricePerKWh { get; init; }

    /// <summary>
    /// Price per kWh for selling energy to grid in EUR
    /// </summary>
    public double SellPricePerKWh { get; init; }

    /// <summary>
    /// List of revenue data points for the specified period
    /// </summary>
    public List<EnergyRevenueDataPoint> Data { get; init; } = new();
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
    /// Calculated revenue/cost in EUR
    /// Formula: (Sell × SellPrice) - (Buy × BuyPrice)
    /// Positive = revenue/profit, Negative = cost/loss
    /// </summary>
    public double Price { get; init; }
}
