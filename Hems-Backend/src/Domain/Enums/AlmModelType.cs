namespace Hems_Backend.Domain.Enums;

/// <summary>
/// ALM (AI Learning Module) supported model types
/// </summary>
public enum AlmModelType
{
    /// <summary>
    /// Power consumption prediction using LSTM model
    /// Predicts LoadTotalPowerW values
    /// </summary>
    PowerConsumption = 1,

    // Future model types can be added here
    // PvGeneration = 2,
    // BatteryOptimization = 3,
}
