using System.Text.Json;

namespace Hems.DeviceSimulator.Services;

/// <summary>
/// Günlük birikimli enerji verisi (Her saat başı gönderilir)
/// </summary>
public class DailyCumulativeEnergyData
{
    public DateTime DateTime { get; set; }
    public Guid DeviceId { get; set; }
    
    // Günlük birikimli değerler (kWh)
    public double PvEnergyKWhToday { get; set; }
    public double GridImportEnergyKWhToday { get; set; }
    public double GridExportEnergyKWhToday { get; set; }
    public double GeneratorEnergyKWhToday { get; set; }
    public double BatteryDischargeEnergyKWhToday { get; set; }
    public double BatteryChargeEnergyKWhToday { get; set; }
    public double LoadEnergyKWhToday { get; set; }
}

/// <summary>
/// Ömür boyu enerji verisi (Her gece yarısı gönderilir)
/// </summary>
public class LifetimeEnergyData
{
    public DateTime DateTime { get; set; }
    public Guid DeviceId { get; set; }
    
    // Ömür boyu toplam değerler (kWh)
    public double PvEnergyKWhTotal { get; set; }
    public double GridImportEnergyKWhTotal { get; set; }
    public double GridExportEnergyKWhTotal { get; set; }
    public double GeneratorEnergyKWhTotal { get; set; }
    public double BatteryDischargeEnergyKWhTotal { get; set; }
    public double BatteryChargeEnergyKWhTotal { get; set; }
    public double LoadEnergyKWhTotal { get; set; }
}

/// <summary>
/// Device'ın enerji durumu ve geçmişi
/// </summary>
public class DeviceEnergyState
{
    public Guid DeviceId { get; set; }
    public DateTime LastUpdate { get; set; }
    
    // Günlük birikimli değerler
    public double PvEnergyKWhToday { get; set; }
    public double GridImportEnergyKWhToday { get; set; }
    public double GridExportEnergyKWhToday { get; set; }
    public double GeneratorEnergyKWhToday { get; set; }
    public double BatteryDischargeEnergyKWhToday { get; set; }
    public double BatteryChargeEnergyKWhToday { get; set; }
    public double LoadEnergyKWhToday { get; set; }
    
    // Ömür boyu toplam değerler
    public double PvEnergyKWhTotal { get; set; }
    public double GridImportEnergyKWhTotal { get; set; }
    public double GridExportEnergyKWhTotal { get; set; }
    public double GeneratorEnergyKWhTotal { get; set; }
    public double BatteryDischargeEnergyKWhTotal { get; set; }
    public double BatteryChargeEnergyKWhTotal { get; set; }
    public double LoadEnergyKWhTotal { get; set; }
    
    // Güç değerleri (kW) - simulator için gerekli
    public double PvPowerKw { get; set; }
    public double GridImportPowerKw { get; set; }
    public double GridExportPowerKw { get; set; }
    public double GeneratorPowerKw { get; set; }
    public double BatteryDischargePowerKw { get; set; }
    public double BatteryChargePowerKw { get; set; }
    public double LoadPowerKw { get; set; }
}
