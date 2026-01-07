using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.PowerFlow.Common.Models;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Provides simulated data for demo users.
/// Generates realistic power flow, energy, and weather data based on time of day.
/// </summary>
public interface IDemoSimulationDataService
{
    /// <summary>
    /// Gets simulated PowerFlow data for the current moment
    /// </summary>
    PowerFlowDto GetCurrentPowerFlow(Guid deviceId);

    /// <summary>
    /// Gets simulated PowerFlow data for a specific time
    /// </summary>
    PowerFlowDto GetPowerFlowAtTime(Guid deviceId, DateTime timestamp);

    /// <summary>
    /// Gets simulated daily energy data for a specific date
    /// </summary>
    DailyEnergyData GetDailyEnergy(DateTime date);

    /// <summary>
    /// Gets simulated energy prices based on Netherlands market
    /// </summary>
    EnergyPriceData GetEnergyPrices(DateTime timestamp);

    /// <summary>
    /// Gets simulated weather data
    /// </summary>
    DemoWeatherData GetWeatherData();

    /// <summary>
    /// Gets battery state of charge percentage
    /// </summary>
    double GetBatterySoc(DateTime timestamp);
}

public class DemoSimulationDataService : IDemoSimulationDataService
{
    private readonly ILogger<DemoSimulationDataService> _logger;
    private readonly Random _random = new();

    // Netherlands electricity prices (EUR/kWh)
    private readonly TimeOfUsePricing[] _pricingSchedule = new[]
    {
        new TimeOfUsePricing(0, 6, 0.22m, 0.07m),    // Off-Peak Night
        new TimeOfUsePricing(6, 9, 0.35m, 0.11m),    // Morning Peak
        new TimeOfUsePricing(9, 17, 0.28m, 0.09m),   // Day Standard
        new TimeOfUsePricing(17, 21, 0.42m, 0.13m),  // Evening Peak
        new TimeOfUsePricing(21, 24, 0.25m, 0.08m)   // Late Evening
    };

    public DemoSimulationDataService(ILogger<DemoSimulationDataService> logger)
    {
        _logger = logger;
    }

    public PowerFlowDto GetCurrentPowerFlow(Guid deviceId)
    {
        return GetPowerFlowAtTime(deviceId, DateTime.UtcNow);
    }

    public PowerFlowDto GetPowerFlowAtTime(Guid deviceId, DateTime timestamp)
    {
        var hour = timestamp.Hour;
        var minute = timestamp.Minute;
        var timeOfDay = hour + (minute / 60.0);

        // Calculate PV power based on time of day (bell curve centered at solar noon ~13:00)
        double pvPowerKw = CalculatePvPower(timeOfDay);

        // Calculate load based on time of day (higher morning/evening, lower night)
        double loadPowerKw = CalculateLoadPower(timeOfDay);

        // Calculate battery and grid power based on energy balance
        var (gridPowerKw, batteryPowerKw) = CalculateEnergyBalance(pvPowerKw, loadPowerKw, timestamp);

        // Add small random variation for realism
        pvPowerKw = AddVariation(pvPowerKw, 0.05);
        loadPowerKw = AddVariation(loadPowerKw, 0.03);

        _logger.LogDebug("Demo PowerFlow: PV={PV:F2}kW, Load={Load:F2}kW, Grid={Grid:F2}kW, Battery={Battery:F2}kW",
            pvPowerKw, loadPowerKw, gridPowerKw, batteryPowerKw);

        return new PowerFlowDto
        {
            TimestampUtc = new DateTimeOffset(timestamp, TimeSpan.Zero),
            DeviceId = deviceId,
            SolarPowerKw = Math.Round(pvPowerKw, 3),
            GridPowerKw = Math.Round(gridPowerKw, 3),
            LoadPowerKw = Math.Round(loadPowerKw, 3),
            BatteryPowerKw = Math.Round(batteryPowerKw, 3),
            GeneratorPowerKw = 0
        };
    }

    public DailyEnergyData GetDailyEnergy(DateTime date)
    {
        // Get seasonal multiplier based on month
        var seasonalPvMultiplier = GetSeasonalPvMultiplier(date.Month);
        var seasonalLoadMultiplier = GetSeasonalLoadMultiplier(date.Month);

        // Get day type (using date as seed for consistency)
        var dayTypeSeed = date.Year * 1000 + date.DayOfYear;
        var dayRandom = new Random(dayTypeSeed);
        var dayType = GetDayType(dayRandom.NextDouble());

        // Base values for sunny day
        var basePv = 32.5;
        var baseLoad = 18.0;
        var baseGridImport = 2.5;
        var baseGridExport = 12.0;
        var baseBatteryCharge = 8.0;
        var baseBatteryDischarge = 6.0;

        // Apply day type multipliers
        var (pvMult, importMult, exportMult) = GetDayTypeMultipliers(dayType);

        // Calculate final values
        var pvEnergy = basePv * seasonalPvMultiplier * pvMult * (0.9 + dayRandom.NextDouble() * 0.2);
        var loadEnergy = baseLoad * seasonalLoadMultiplier * (0.9 + dayRandom.NextDouble() * 0.2);
        var gridImport = baseGridImport * importMult * (0.8 + dayRandom.NextDouble() * 0.4);
        var gridExport = baseGridExport * exportMult * (0.8 + dayRandom.NextDouble() * 0.4);
        var batteryCharge = baseBatteryCharge * pvMult * (0.8 + dayRandom.NextDouble() * 0.4);
        var batteryDischarge = baseBatteryDischarge * (0.8 + dayRandom.NextDouble() * 0.4);

        // Ensure energy balance makes sense
        if (pvEnergy < loadEnergy)
        {
            gridImport = Math.Max(gridImport, (loadEnergy - pvEnergy) * 0.6);
            gridExport = Math.Min(gridExport, pvEnergy * 0.1);
        }

        return new DailyEnergyData
        {
            Date = date,
            PvEnergyKWh = Math.Round(pvEnergy, 3),
            LoadEnergyKWh = Math.Round(loadEnergy, 3),
            GridImportEnergyKWh = Math.Round(gridImport, 3),
            GridExportEnergyKWh = Math.Round(gridExport, 3),
            BatteryChargeEnergyKWh = Math.Round(batteryCharge, 3),
            BatteryDischargeEnergyKWh = Math.Round(batteryDischarge, 3),
            GeneratorEnergyKWh = 0
        };
    }

    public EnergyPriceData GetEnergyPrices(DateTime timestamp)
    {
        var hour = timestamp.Hour;
        var pricing = _pricingSchedule.First(p => hour >= p.BeginHour && hour < p.EndHour);

        // Apply monthly variation
        var monthMultiplier = GetMonthlyPriceMultiplier(timestamp.Month);

        return new EnergyPriceData
        {
            BuyPrice = pricing.BuyPrice * monthMultiplier,
            SellPrice = pricing.SellPrice * monthMultiplier,
            Currency = "EUR",
            Unit = "kWh",
            BeginHour = pricing.BeginHour,
            EndHour = pricing.EndHour
        };
    }

    public DemoWeatherData GetWeatherData()
    {
        var hour = DateTime.UtcNow.Hour;

        // Temperature varies by time of day
        var baseTemp = 15 + 6 * Math.Sin((hour - 6) * Math.PI / 12);
        if (hour < 6 || hour > 20) baseTemp = 12 + _random.NextDouble() * 4;

        return new DemoWeatherData
        {
            Condition = "Partly Cloudy",
            TemperatureC = Math.Round(baseTemp + _random.NextDouble() * 2 - 1, 1),
            Humidity = 60 + (int)(_random.NextDouble() * 20),
            WindSpeedKph = 10 + _random.NextDouble() * 15,
            CloudCover = 25 + (int)(_random.NextDouble() * 30),
            UvIndex = hour >= 10 && hour <= 16 ? 4 + (int)(_random.NextDouble() * 3) : 1
        };
    }

    public double GetBatterySoc(DateTime timestamp)
    {
        var hour = timestamp.Hour;

        // Battery SOC pattern: low at morning, charges during day, discharges evening
        return hour switch
        {
            >= 0 and < 6 => 40 + _random.NextDouble() * 15,      // Night: 40-55%
            >= 6 and < 9 => 30 + _random.NextDouble() * 15,      // Morning: 30-45%
            >= 9 and < 12 => 50 + _random.NextDouble() * 25,     // Late morning: 50-75%
            >= 12 and < 15 => 75 + _random.NextDouble() * 20,    // Midday: 75-95%
            >= 15 and < 18 => 85 + _random.NextDouble() * 15,    // Afternoon: 85-100%
            >= 18 and < 21 => 60 + _random.NextDouble() * 25,    // Evening: 60-85%
            _ => 45 + _random.NextDouble() * 20                   // Late evening: 45-65%
        };
    }

    #region Private Helper Methods

    private double CalculatePvPower(double timeOfDay)
    {
        // No production before sunrise (6:00) or after sunset (20:00)
        if (timeOfDay < 6 || timeOfDay > 20) return 0;

        // Bell curve centered at solar noon (13:00)
        var solarNoon = 13.0;
        var hourFromNoon = Math.Abs(timeOfDay - solarNoon);

        // Max power at noon, drops to near zero at sunrise/sunset
        // Using Gaussian-like curve for realistic solar production
        var maxPower = 5.0; // 5kW system
        var sigma = 4.0;    // Spread of the curve

        var power = maxPower * Math.Exp(-(hourFromNoon * hourFromNoon) / (2 * sigma * sigma));

        // Add cloud variation (0.7-1.0 multiplier)
        var cloudFactor = 0.85 + _random.NextDouble() * 0.15;

        return Math.Max(0, power * cloudFactor);
    }

    private double CalculateLoadPower(double timeOfDay)
    {
        // Load pattern: base load + time-of-day variation
        return timeOfDay switch
        {
            >= 0 and < 5 => 0.3 + _random.NextDouble() * 0.2,      // Night: 0.3-0.5 kW
            >= 5 and < 7 => 0.5 + _random.NextDouble() * 0.5,      // Early morning: 0.5-1.0 kW
            >= 7 and < 9 => 1.0 + _random.NextDouble() * 0.8,      // Morning peak: 1.0-1.8 kW
            >= 9 and < 12 => 0.6 + _random.NextDouble() * 0.4,     // Late morning: 0.6-1.0 kW
            >= 12 and < 14 => 0.8 + _random.NextDouble() * 0.5,    // Lunch: 0.8-1.3 kW
            >= 14 and < 17 => 0.5 + _random.NextDouble() * 0.4,    // Afternoon: 0.5-0.9 kW
            >= 17 and < 20 => 1.5 + _random.NextDouble() * 1.2,    // Evening peak: 1.5-2.7 kW
            >= 20 and < 22 => 1.0 + _random.NextDouble() * 0.8,    // Late evening: 1.0-1.8 kW
            _ => 0.4 + _random.NextDouble() * 0.3                   // Night: 0.4-0.7 kW
        };
    }

    private (double gridPowerKw, double batteryPowerKw) CalculateEnergyBalance(
        double pvPowerKw, double loadPowerKw, DateTime timestamp)
    {
        var netPower = pvPowerKw - loadPowerKw;
        var batterySoc = GetBatterySoc(timestamp);

        double gridPowerKw;
        double batteryPowerKw;

        if (netPower >= 0)
        {
            // Excess PV: charge battery first, then export to grid
            var excessPower = netPower;

            // Battery charging (max 3kW, only if not full)
            var maxChargeRate = 3.0;
            var chargeCapacityFactor = Math.Max(0, (100 - batterySoc) / 100);
            var batteryCharge = Math.Min(excessPower * 0.7, maxChargeRate * chargeCapacityFactor);
            batteryPowerKw = -batteryCharge; // Negative = charging

            // Remaining excess goes to grid (negative = export)
            gridPowerKw = -(excessPower - batteryCharge);
        }
        else
        {
            // Deficit: discharge battery first, then import from grid
            var deficit = Math.Abs(netPower);

            // Battery discharging (max 2.5kW, only if not empty)
            var maxDischargeRate = 2.5;
            var dischargeCapacityFactor = Math.Min(1, batterySoc / 100);
            var batteryDischarge = Math.Min(deficit * 0.6, maxDischargeRate * dischargeCapacityFactor);
            batteryPowerKw = batteryDischarge; // Positive = discharging

            // Remaining deficit from grid (positive = import)
            gridPowerKw = deficit - batteryDischarge;
        }

        return (gridPowerKw, batteryPowerKw);
    }

    private double AddVariation(double value, double variationPercent)
    {
        var variation = value * variationPercent * (2 * _random.NextDouble() - 1);
        return Math.Max(0, value + variation);
    }

    private double GetSeasonalPvMultiplier(int month)
    {
        return month switch
        {
            1 => 0.25,
            2 => 0.35,
            3 => 0.55,
            4 => 0.80,
            5 => 1.00,
            6 => 1.15,
            7 => 1.10,
            8 => 1.00,
            9 => 0.75,
            10 => 0.50,
            11 => 0.30,
            12 => 0.20,
            _ => 1.0
        };
    }

    private double GetSeasonalLoadMultiplier(int month)
    {
        return month switch
        {
            1 => 1.30,
            2 => 1.25,
            3 => 1.10,
            4 => 0.95,
            5 => 0.85,
            6 => 0.80,
            7 => 0.82,
            8 => 0.85,
            9 => 0.95,
            10 => 1.10,
            11 => 1.25,
            12 => 1.35,
            _ => 1.0
        };
    }

    private decimal GetMonthlyPriceMultiplier(int month)
    {
        return month switch
        {
            1 => 1.15m,
            2 => 1.10m,
            3 => 1.05m,
            4 => 0.95m,
            5 => 0.90m,
            6 => 0.88m,
            7 => 0.90m,
            8 => 0.92m,
            9 => 0.98m,
            10 => 1.05m,
            11 => 1.12m,
            12 => 1.18m,
            _ => 1.0m
        };
    }

    private string GetDayType(double randomValue)
    {
        return randomValue switch
        {
            < 0.30 => "sunny",
            < 0.65 => "partlyCloudy",
            < 0.90 => "cloudy",
            _ => "rainy"
        };
    }

    private (double pvMult, double importMult, double exportMult) GetDayTypeMultipliers(string dayType)
    {
        return dayType switch
        {
            "sunny" => (1.0, 0.3, 1.0),
            "partlyCloudy" => (0.7, 0.6, 0.6),
            "cloudy" => (0.35, 1.0, 0.2),
            "rainy" => (0.15, 1.5, 0.05),
            _ => (0.7, 0.6, 0.6)
        };
    }

    #endregion
}

#region Data Transfer Objects

public record DailyEnergyData
{
    public DateTime Date { get; init; }
    public double PvEnergyKWh { get; init; }
    public double LoadEnergyKWh { get; init; }
    public double GridImportEnergyKWh { get; init; }
    public double GridExportEnergyKWh { get; init; }
    public double BatteryChargeEnergyKWh { get; init; }
    public double BatteryDischargeEnergyKWh { get; init; }
    public double GeneratorEnergyKWh { get; init; }
}

public record EnergyPriceData
{
    public decimal BuyPrice { get; init; }
    public decimal SellPrice { get; init; }
    public string Currency { get; init; } = "EUR";
    public string Unit { get; init; } = "kWh";
    public int BeginHour { get; init; }
    public int EndHour { get; init; }
}

public record DemoWeatherData
{
    public string Condition { get; init; } = string.Empty;
    public double TemperatureC { get; init; }
    public int Humidity { get; init; }
    public double WindSpeedKph { get; init; }
    public int CloudCover { get; init; }
    public int UvIndex { get; init; }
}

public record TimeOfUsePricing(int BeginHour, int EndHour, decimal BuyPrice, decimal SellPrice);

#endregion
