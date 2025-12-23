using Hems_Backend.Domain.Common;

namespace Hems_Backend.Domain.Entities;

/// <summary>
/// Stores historical sell and buy prices for a device.
/// Each record represents a price point that was effective from the Created timestamp
/// until a newer price record was created.
/// </summary>
public class DevicePrice : BaseGuidAuditableEntity
{
    public Guid DeviceId { get; set; }

    /// <summary>
    /// The unit of energy measurement (e.g., "kWh")
    /// </summary>
    public string Unit { get; set; } = "kWh";

    /// <summary>
    /// Price at which energy is sold to the grid
    /// </summary>
    public decimal SellPrice { get; set; }

    /// <summary>
    /// Price at which energy is bought from the grid
    /// </summary>
    public decimal BuyPrice { get; set; }

    /// <summary>
    /// Currency code (e.g., "EUR", "USD", "TRY")
    /// </summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// When this price becomes effective. Defaults to Created time.
    /// Allows setting future-dated prices.
    /// </summary>
    public DateTimeOffset EffectiveFrom { get; set; }

    /// <summary>
    /// Optional: When this price stops being effective.
    /// Null means it's effective until a newer price is set.
    /// </summary>
    public DateTimeOffset? EffectiveTo { get; set; }

    /// <summary>
    /// Start hour of the day when this price is valid (0-23).
    /// Null means the price starts from 00:00.
    /// </summary>
    public int? BeginHour { get; set; }

    /// <summary>
    /// End hour of the day when this price is valid (0-23).
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
