using Hems_Backend.Domain.Common;

namespace Hems_Backend.Domain.Entities;

/// <summary>
/// Stores energy price configurations for a device.
/// Each record represents a price entry for a specific time-of-day range.
/// </summary>
public class EnergyPriceHistory : BaseGuidAuditableEntity
{
    public Guid DeviceId { get; set; }

    /// <summary>
    /// The unit of energy measurement (e.g., "kWh")
    /// </summary>
    public string Unit { get; set; } = "kWh";

    /// <summary>
    /// Currency code (e.g., "EUR", "USD", "TRY")
    /// </summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Price at which energy is sold to the grid (export)
    /// </summary>
    public decimal SellPrice { get; set; }

    /// <summary>
    /// Price at which energy is bought from the grid (import)
    /// </summary>
    public decimal BuyPrice { get; set; }

    /// <summary>
    /// Start hour of the day when this price is valid (0-23).
    /// Null means the price starts from 00:00.
    /// </summary>
    public int? BeginHour { get; set; }

    /// <summary>
    /// End hour of the day when this price is valid (0-24).
    /// Null means the price is valid until 23:59.
    /// If both BeginHour and EndHour are null, the price applies all day.
    /// </summary>
    public int? EndHour { get; set; }

    /// <summary>
    /// Whether this price record is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation property
    public Device Device { get; set; } = null!;
}
