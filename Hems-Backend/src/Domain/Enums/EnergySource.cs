namespace Hems_Backend.Domain.Enums;

/// <summary>
/// Represents the source of energy in the system
/// Used for load energy attribution (pie chart)
/// </summary>
public enum EnergySource
{
    /// <summary>
    /// Solar/Photovoltaic energy source
    /// </summary>
    Solar = 1,

    /// <summary>
    /// Battery discharge energy source
    /// </summary>
    Battery = 2,

    /// <summary>
    /// Grid import energy source
    /// </summary>
    Grid = 3,

    /// <summary>
    /// Generator energy source
    /// </summary>
    Generator = 4
}
