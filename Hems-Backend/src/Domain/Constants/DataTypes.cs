namespace Hems_Backend.Domain.Constants;

/// <summary>
/// Constants for DataType field values across different telemetry tables
/// </summary>
public static class DataTypes
{
    // DeviceEnergyData DataTypes
    public const string DailyCumulativeEnergy = "DailyCumulativeEnergyData";
    public const string LifetimeEnergy = "LifetimeEnergyData";
    
    // DevicePowerData DataTypes  
    public const string PowerFlow = "PowerFlow";
    
    // DeviceData DataTypes (raw JSON storage)
    public const string BatteryData = "BatteryData";
    public const string BatterySoc = "BatterySoc";
    public const string BatterySoh = "BatterySoh";
    public const string BatteryClusterData = "BatteryClusterData";
    public const string InverterData = "InverterData";
    public const string LoadData = "LoadData";
    public const string GeneratorData = "GeneratorData";
    public const string AlarmData = "AlarmData";
}

/// <summary>
/// Constants for MessageType field values
/// </summary>
public static class MessageTypes
{
    public const string Telemetry = "Telemetry";
    public const string RPC = "RPC";
    public const string Attributes = "Attributes";
}

/// <summary>
/// Constants for PhaseType field values
/// </summary>
public static class PhaseTypes
{
    public const string SinglePhase = "SinglePhase";
    public const string ThreePhase = "ThreePhase";
}

/// <summary>
/// Constants for CreatedBy field values in energy data
/// </summary>
public static class EnergyDataSources
{
    /// <summary>Data received from real device (telemetry or RPC)</summary>
    public const string Device = "Device";

    /// <summary>Data estimated from power readings when device is unavailable</summary>
    public const string SystemEstimate = "SystemEstimate";

    /// <summary>Data from device simulator</summary>
    public const string Simulator = "Simulator";
}
