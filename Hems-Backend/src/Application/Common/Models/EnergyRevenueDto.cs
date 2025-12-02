namespace Hems_Backend.Application.Common.Models;

/// <summary>
/// Energy revenue summary showing buy/sell energy and calculated revenue
/// </summary>
public record EnergyRevenueDto
{
    /// <summary>
    /// Total energy purchased from grid in kWh
    /// </summary>
    public double Buy { get; init; }

    /// <summary>
    /// Total energy sold to grid in kWh
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

    /// <summary>
    /// Price per kWh for buying energy from grid in EUR
    /// </summary>
    public double BuyPricePerKWh { get; init; }

    /// <summary>
    /// Price per kWh for selling energy to grid in EUR
    /// </summary>
    public double SellPricePerKWh { get; init; }
}
