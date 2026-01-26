using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.PowerFlow.Common.Models;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

// TODO: This service will be transferred to a separate service Hems.Simulator in the future.
// Currently, all demo/simulated data generation is centralized here to avoid database storage
// for demo devices. When migrated to Hems.Simulator, this service will provide data via API
// rather than in-process generation.
//
// Key principle: Demo devices should NEVER store data in the database. All data for demo
// devices should be generated on-the-fly by this service.
/// <summary>
/// Centralized service for generating simulated data for demo devices.
/// All demo data is generated on-demand - no database storage.
/// </summary>
public class DemoSimulationDataService : IDemoSimulationDataService
{
    private readonly ILogger<DemoSimulationDataService> _logger;
    private readonly Random _random = new();

    // In-memory storage for demo device prices (cleared when app restarts)
    private static readonly Dictionary<Guid, DemoPriceSettings> _demoPrices = new();
    private static readonly object _pricesLock = new();

    // Default Netherlands electricity prices (EUR/kWh)
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

    #region Demo Price Management

    public void SetDemoPrices(Guid deviceId, List<DemoPriceEntry> prices, string unit = "kWh", string currency = "EUR")
    {
        lock (_pricesLock)
        {
            _demoPrices[deviceId] = new DemoPriceSettings
            {
                Unit = unit,
                Currency = currency,
                Prices = prices,
                IsCustom = true,
                Created = DateTimeOffset.UtcNow
            };
        }

        _logger.LogInformation("Set {Count} custom prices for demo device {DeviceId}", prices.Count, deviceId);
    }

    public DemoPriceSettings GetDemoPrices(Guid deviceId)
    {
        lock (_pricesLock)
        {
            if (_demoPrices.TryGetValue(deviceId, out var customPrices))
            {
                return customPrices;
            }
        }

        // Return default Netherlands market prices
        return GetDefaultNetherlandsPrices();
    }

    public bool HasCustomPrices(Guid deviceId)
    {
        lock (_pricesLock)
        {
            return _demoPrices.ContainsKey(deviceId);
        }
    }

    public void ClearDemoPrices(Guid deviceId)
    {
        lock (_pricesLock)
        {
            _demoPrices.Remove(deviceId);
        }

        _logger.LogInformation("Cleared custom prices for demo device {DeviceId}", deviceId);
    }

    private DemoPriceSettings GetDefaultNetherlandsPrices()
    {
        return new DemoPriceSettings
        {
            Unit = "kWh",
            Currency = "EUR",
            Prices = _pricingSchedule.Select(p => new DemoPriceEntry
            {
                SellPrice = p.SellPrice,
                BuyPrice = p.BuyPrice,
                BeginHour = p.BeginHour,
                EndHour = p.EndHour
            }).ToList(),
            IsCustom = false,
            Created = DateTimeOffset.UtcNow.AddDays(-30)
        };
    }

    /// <summary>
    /// Gets the buy/sell price for a specific hour from demo prices
    /// </summary>
    public (decimal SellPrice, decimal BuyPrice) GetPriceForHour(Guid deviceId, int hour)
    {
        var priceSettings = GetDemoPrices(deviceId);

        var matchingPrice = priceSettings.Prices
            .FirstOrDefault(p =>
                (!p.BeginHour.HasValue && !p.EndHour.HasValue) ||
                (p.BeginHour.HasValue && p.EndHour.HasValue &&
                 hour >= p.BeginHour.Value && hour < p.EndHour.Value));

        if (matchingPrice != null)
        {
            return (matchingPrice.SellPrice, matchingPrice.BuyPrice);
        }

        // Fallback to first price or default
        var fallback = priceSettings.Prices.FirstOrDefault();
        return fallback != null
            ? (fallback.SellPrice, fallback.BuyPrice)
            : (0.09m, 0.28m);
    }

    /// <summary>
    /// Gets weighted average daily price from demo prices
    /// </summary>
    public (decimal SellPrice, decimal BuyPrice) GetWeightedDailyAveragePrice(Guid deviceId)
    {
        var priceSettings = GetDemoPrices(deviceId);

        if (!priceSettings.Prices.Any())
        {
            return (0.09m, 0.28m);
        }

        // Calculate weighted average based on hours covered
        decimal totalSell = 0;
        decimal totalBuy = 0;
        int totalHours = 0;

        foreach (var price in priceSettings.Prices.Where(p => p.BeginHour.HasValue && p.EndHour.HasValue))
        {
            var hours = price.EndHour!.Value - price.BeginHour!.Value;
            totalSell += price.SellPrice * hours;
            totalBuy += price.BuyPrice * hours;
            totalHours += hours;
        }

        if (totalHours == 0)
        {
            var first = priceSettings.Prices.First();
            return (first.SellPrice, first.BuyPrice);
        }

        return (totalSell / totalHours, totalBuy / totalHours);
    }

    #endregion

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
        // Biased towards sunny/partly cloudy for demo (to show positive revenue)
        var dayTypeSeed = date.Year * 1000 + date.DayOfYear;
        var dayRandom = new Random(dayTypeSeed);
        var dayType = GetDemoFavorableDayType(dayRandom.NextDouble());

        // Base values for a 5kW PV system on a sunny summer day
        // Optimized for demo to show positive revenue
        var basePvSunny = 28.0;  // kWh - peak summer sunny day (slightly optimistic)
        var baseLoad = 10.0;     // kWh - efficient household daily consumption

        // Apply day type multipliers for PV (more favorable for demo)
        var pvMult = dayType switch
        {
            "sunny" => 1.0,
            "partlyCloudy" => 0.75,  // Better yield on partly cloudy
            "cloudy" => 0.40,
            "rainy" => 0.25,
            _ => 0.75
        };

        // Calculate PV and Load with variation
        var pvEnergy = basePvSunny * seasonalPvMultiplier * pvMult * (0.95 + dayRandom.NextDouble() * 0.1);
        var loadEnergy = baseLoad * seasonalLoadMultiplier * (0.85 + dayRandom.NextDouble() * 0.2);

        // Calculate net energy (positive = excess, negative = deficit)
        var netEnergy = pvEnergy - loadEnergy;

        // Initialize values
        double gridImport;
        double gridExport;
        double batteryCharge;
        double batteryDischarge;

        if (netEnergy >= 0)
        {
            // PV produces more than load - export excess
            // Battery absorbs some during day, releases in evening
            batteryCharge = Math.Min(netEnergy * 0.35, 8.0);  // Max 8 kWh battery capacity
            batteryDischarge = batteryCharge * 0.90;  // 90% round-trip efficiency

            // Grid: minimal import at night, maximize export during day
            gridImport = loadEnergy * 0.05 * (0.8 + dayRandom.NextDouble() * 0.4);  // ~5% of load from grid at night
            gridExport = Math.Max(0, netEnergy - batteryCharge + batteryDischarge + pvEnergy * 0.1);  // Export excess + bonus
        }
        else
        {
            // Load exceeds PV - need to import
            // But we still try to export during peak PV hours for positive revenue
            var deficit = Math.Abs(netEnergy);

            // Battery helps cover the deficit efficiently
            batteryDischarge = Math.Min(deficit * 0.5, 6.0);  // Battery covers up to 50% of deficit
            batteryCharge = Math.Min(pvEnergy * 0.3, 4.0);  // Charge from PV peak

            // Grid: minimize import, still export during peak sun hours
            gridImport = Math.Max(0, deficit - batteryDischarge) * 0.6;  // Only 60% of remaining deficit
            gridExport = pvEnergy * 0.4 * (0.8 + dayRandom.NextDouble() * 0.4);  // Export 40% of PV during peak
        }

        // Ensure positive revenue: GridExport * 0.09 > GridImport * 0.28
        // So GridExport should be > 3.1 * GridImport
        // Apply adjustment factor to ensure this
        if (gridExport < gridImport * 3.5)
        {
            gridExport = gridImport * 3.5 + dayRandom.NextDouble() * 2;
        }

        // Add small random variation
        gridImport *= (0.95 + dayRandom.NextDouble() * 0.1);
        gridExport *= (0.98 + dayRandom.NextDouble() * 0.1);

        return new DailyEnergyData
        {
            Date = date,
            PvEnergyKWh = Math.Round(pvEnergy, 2),
            LoadEnergyKWh = Math.Round(loadEnergy, 2),
            GridImportEnergyKWh = Math.Round(Math.Max(0, gridImport), 2),
            GridExportEnergyKWh = Math.Round(Math.Max(0, gridExport), 2),
            BatteryChargeEnergyKWh = Math.Round(batteryCharge, 2),
            BatteryDischargeEnergyKWh = Math.Round(batteryDischarge, 2),
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

    /// <summary>
    /// Gets day type biased towards favorable weather for demo purposes.
    /// More sunny/partly cloudy days to ensure positive revenue.
    /// </summary>
    private string GetDemoFavorableDayType(double randomValue)
    {
        return randomValue switch
        {
            < 0.50 => "sunny",          // 50% sunny (was 30%)
            < 0.80 => "partlyCloudy",   // 30% partly cloudy (was 35%)
            < 0.95 => "cloudy",         // 15% cloudy (was 25%)
            _ => "rainy"                // 5% rainy (was 10%)
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

    #region Demo Device Details Generation

    public List<DemoDetailItem> GetDemoBatteryRealtimeDetails()
    {
        var details = new List<DemoDetailItem>();

        // Get SOC (consistent with dashboard)
        var soc = GetBatterySoc(DateTime.UtcNow);

        // State of Charge
        details.Add(new DemoDetailItem
        {
            Key = "soc",
            Value = Math.Round(soc, 1),
            Unit = "%",
            LabelKey = "realtime.soc"
        });

        // State of Health (simulated - batteries typically maintain 95%+ for first few years)
        details.Add(new DemoDetailItem
        {
            Key = "soh",
            Value = 98.5,
            Unit = "%",
            LabelKey = "realtime.soh"
        });

        // Battery voltage - realistic for a 51.2V LiFePO4 battery pack
        // Voltage varies with SOC: ~48V at 0%, ~54V at 100%
        var voltage = 48.0 + (soc / 100.0) * 6.0;
        details.Add(new DemoDetailItem
        {
            Key = "voltage",
            Value = Math.Round(voltage, 2),
            Unit = "V",
            LabelKey = "realtime.voltage"
        });

        // Battery current - based on current power flow
        // Positive = discharging, Negative = charging (matches RPC convention)
        var powerFlow = GetCurrentPowerFlow(Guid.Empty);
        var batteryCurrent = (powerFlow.BatteryPowerKw * 1000) / voltage; // I = P/V
        details.Add(new DemoDetailItem
        {
            Key = "current",
            Value = Math.Round(batteryCurrent, 2),
            Unit = "A",
            LabelKey = "realtime.current"
        });

        // Battery temperature (realistic range 20-35°C during operation)
        var hour = DateTime.UtcNow.Hour;
        var baseTemp = 25.0 + Math.Sin((hour - 6) * Math.PI / 12) * 5; // Varies with time of day
        details.Add(new DemoDetailItem
        {
            Key = "temperature",
            Value = Math.Round(baseTemp, 1),
            Unit = "°C",
            LabelKey = "realtime.temperature"
        });

        // BMS Count - simulating 2 battery modules
        details.Add(new DemoDetailItem
        {
            Key = "bmsCount",
            Value = 2,
            Unit = null,
            LabelKey = "realtime.bmsCount"
        });

        // BMS List - individual module details for 2 simulated BMS modules
        var bmsList = GenerateDemoBmsList(soc, voltage, batteryCurrent, baseTemp);
        details.Add(new DemoDetailItem
        {
            Key = "bmsList",
            Value = bmsList,
            Unit = null,
            LabelKey = "realtime.bmsList"
        });

        return details;
    }

    /// <summary>
    /// Generates simulated BMS list with 2 battery modules
    /// </summary>
    private List<Dictionary<string, object?>> GenerateDemoBmsList(double soc, double voltage, double totalCurrent, double baseTemp)
    {
        var bmsList = new List<Dictionary<string, object?>>();

        // Simulate 2 BMS modules with slightly different values
        for (int i = 1; i <= 2; i++)
        {
            var moduleVariation = (i - 1.5) * 0.02; // Small variation between modules
            var moduleSoc = Math.Round(soc + moduleVariation * 10, 1);
            var moduleVoltage = Math.Round(voltage + moduleVariation * 0.5, 2);
            var moduleCurrent = Math.Round(totalCurrent / 2 + moduleVariation * 0.3, 2);
            var moduleTemp = Math.Round(baseTemp + (i - 1) * 0.5, 1);

            // Calculate cell voltages based on pack voltage (16 cells in series for 51.2V nominal)
            var avgCellVoltageMv = (int)Math.Round(moduleVoltage / 16 * 1000);
            var cellVariation = 3; // ±3mV variation
            var maxCellVoltageMv = avgCellVoltageMv + cellVariation;
            var minCellVoltageMv = avgCellVoltageMv - cellVariation;

            // Determine charge/discharge status based on current
            // 0 = Idle, 1 = Charging, 2 = Discharging
            int chargeDischargeStatus;
            string statusText;
            if (Math.Abs(moduleCurrent) < 0.1)
            {
                chargeDischargeStatus = 0;
                statusText = "Idle";
            }
            else if (moduleCurrent < 0)
            {
                chargeDischargeStatus = 1;
                statusText = "Charging";
            }
            else
            {
                chargeDischargeStatus = 2;
                statusText = "Discharging";
            }

            // Remaining capacity based on SOC (100Ah rated capacity)
            var ratedCapacityAh = 100.0;
            var remainingCapacityAh = Math.Round(ratedCapacityAh * moduleSoc / 100, 2);

            bmsList.Add(new Dictionary<string, object?>
            {
                { "bmsAddress", i },
                { "chargeDischargeStatus", chargeDischargeStatus },
                { "chargeDischargeStatusText", statusText },
                { "currentA", moduleCurrent },
                { "voltageV", moduleVoltage },
                { "remainingCapacityAh", remainingCapacityAh },
                { "maxCellVoltageMv", maxCellVoltageMv },
                { "minCellVoltageMv", minCellVoltageMv },
                { "avgCellVoltageMv", avgCellVoltageMv },
                { "avgCellTempC", moduleTemp },
                { "ratedCapacityAh", ratedCapacityAh },
                { "totalDischargeWh", 1264 + (i - 1) * 87 }, // Slightly different discharge history
                { "cycleCount", 487 + (i - 1) * 12 }, // Slightly different cycle counts
                { "sohPercent", 98.5 - (i - 1) * 0.3 }, // Slightly different SOH
                { "socPercent", moduleSoc }
            });
        }

        return bmsList;
    }

    public List<DemoDetailItem> GetDemoBatteryParameterDetails(string? name, string? manufacturer, string? model, string? serialNumber)
    {
        var details = new List<DemoDetailItem>
        {
            new DemoDetailItem
            {
                Key = "name",
                Value = name ?? "Demo Battery",
                Unit = null,
                LabelKey = "parameter.name"
            },
            new DemoDetailItem
            {
                Key = "manufacturer",
                Value = manufacturer ?? "Pylontech",
                Unit = null,
                LabelKey = "parameter.manufacturer"
            },
            new DemoDetailItem
            {
                Key = "model",
                Value = model ?? "US3000C",
                Unit = null,
                LabelKey = "parameter.model"
            },
            new DemoDetailItem
            {
                Key = "serialNumber",
                Value = serialNumber ?? "DEMO-BAT-001",
                Unit = null,
                LabelKey = "parameter.serialNumber"
            },
            new DemoDetailItem
            {
                Key = "capacity",
                Value = 3.55,
                Unit = "kWh",
                LabelKey = "parameter.capacity"
            },
            new DemoDetailItem
            {
                Key = "ratedVoltage",
                Value = 51.2,
                Unit = "V",
                LabelKey = "parameter.ratedVoltage"
            },
            new DemoDetailItem
            {
                Key = "chemistry",
                Value = "LiFePO4",
                Unit = null,
                LabelKey = "parameter.chemistry"
            },
            new DemoDetailItem
            {
                Key = "firmwareVersion",
                Value = "V2.8",
                Unit = null,
                LabelKey = "parameter.firmwareVersion"
            }
        };

        return details;
    }

    public List<DemoDetailItem> GetDemoInverterRealtimeDetails()
    {
        var details = new List<DemoDetailItem>();

        // Get current power flow for realistic values
        var powerFlow = GetCurrentPowerFlow(Guid.Empty);

        // Run State
        var runState = (powerFlow.SolarPowerKw > 0.1) ? "Normal Operation" : "Standby";
        details.Add(new DemoDetailItem
        {
            Key = "runState",
            Value = runState,
            Unit = null,
            LabelKey = "realtime.runState"
        });

        // Output Active Power (total load power in W)
        details.Add(new DemoDetailItem
        {
            Key = "outputActivePower",
            Value = Math.Round(powerFlow.LoadPowerKw * 1000, 0),
            Unit = "W",
            LabelKey = "realtime.outputActivePower"
        });

        // Output Apparent Power (slightly higher than active due to power factor)
        var apparentPower = powerFlow.LoadPowerKw * 1000 * 1.05;
        details.Add(new DemoDetailItem
        {
            Key = "outputApparentPower",
            Value = Math.Round(apparentPower, 0),
            Unit = "VA",
            LabelKey = "realtime.outputApparentPower"
        });

        // Three-phase output data (European standard 230V ± 5% per phase)
        // Use seeded random for consistent values per call
        var voltageRandom = new Random(DateTime.UtcNow.Hour * 60 + DateTime.UtcNow.Minute / 5);

        // Calculate phase voltages
        var phaseAVoltage = 230.0 + (voltageRandom.NextDouble() * 6 - 3);
        var phaseBVoltage = 230.0 + (voltageRandom.NextDouble() * 6 - 3);
        var phaseCVoltage = 230.0 + (voltageRandom.NextDouble() * 6 - 3);

        // Calculate phase currents (balanced load distribution across 3 phases)
        var totalCurrent = (powerFlow.LoadPowerKw * 1000) / 230.0;
        var phaseACurrent = totalCurrent / 3 * (0.95 + voltageRandom.NextDouble() * 0.1);
        var phaseBCurrent = totalCurrent / 3 * (0.95 + voltageRandom.NextDouble() * 0.1);
        var phaseCCurrent = totalCurrent / 3 * (0.95 + voltageRandom.NextDouble() * 0.1);

        // Calculate phase power (P = V * I)
        var phaseAPower = phaseAVoltage * phaseACurrent;
        var phaseBPower = phaseBVoltage * phaseBCurrent;
        var phaseCPower = phaseCVoltage * phaseCCurrent;

        // Return phases as an array (format expected by frontend)
        var phases = new[]
        {
            new Dictionary<string, object>
            {
                { "Phase Id", 1 },
                { "Voltage", Math.Round(phaseAVoltage, 1) },
                { "Current", Math.Round(phaseACurrent, 2) },
                { "Power", Math.Round(phaseAPower, 0) }
            },
            new Dictionary<string, object>
            {
                { "Phase Id", 2 },
                { "Voltage", Math.Round(phaseBVoltage, 1) },
                { "Current", Math.Round(phaseBCurrent, 2) },
                { "Power", Math.Round(phaseBPower, 0) }
            },
            new Dictionary<string, object>
            {
                { "Phase Id", 3 },
                { "Voltage", Math.Round(phaseCVoltage, 1) },
                { "Current", Math.Round(phaseCCurrent, 2) },
                { "Power", Math.Round(phaseCPower, 0) }
            }
        };

        details.Add(new DemoDetailItem
        {
            Key = "Phases",
            Value = phases,
            Unit = null,
            LabelKey = "realtime.phases"
        });

        // Output Frequency (European standard 50Hz) - single value shared across all phases
        var outputFrequency = 50.0 + (voltageRandom.NextDouble() * 0.2 - 0.1);
        details.Add(new DemoDetailItem
        {
            Key = "outputFrequency",
            Value = Math.Round(outputFrequency, 2),
            Unit = "Hz",
            LabelKey = "realtime.outputFrequency"
        });

        // Grid Side Active Power
        details.Add(new DemoDetailItem
        {
            Key = "gridSideActivePower",
            Value = Math.Round(powerFlow.GridPowerKw * 1000, 0),
            Unit = "W",
            LabelKey = "realtime.gridSideActivePower"
        });

        // Load data (similar to Grid, using LoadPowerKw)
        var loadPowerW = powerFlow.LoadPowerKw * 1000;

        // Load Total Active Power
        details.Add(new DemoDetailItem
        {
            Key = "loadTotalActivePower",
            Value = Math.Round(loadPowerW, 0),
            Unit = "W",
            LabelKey = "realtime.loadTotalActivePower"
        });

        // Load Phase Powers (distributed across 3 phases)
        var loadPhaseAPower = loadPowerW / 3 * (0.9 + voltageRandom.NextDouble() * 0.2);
        var loadPhaseBPower = loadPowerW / 3 * (0.9 + voltageRandom.NextDouble() * 0.2);
        var loadPhaseCPower = loadPowerW - loadPhaseAPower - loadPhaseBPower;

        details.Add(new DemoDetailItem
        {
            Key = "loadPhaseAPower",
            Value = Math.Round(loadPhaseAPower, 0),
            Unit = "W",
            LabelKey = "realtime.loadPhaseAPower"
        });

        details.Add(new DemoDetailItem
        {
            Key = "loadPhaseBPower",
            Value = Math.Round(loadPhaseBPower, 0),
            Unit = "W",
            LabelKey = "realtime.loadPhaseBPower"
        });

        details.Add(new DemoDetailItem
        {
            Key = "loadPhaseCPower",
            Value = Math.Round(loadPhaseCPower, 0),
            Unit = "W",
            LabelKey = "realtime.loadPhaseCPower"
        });

        // Load Phase Voltages (similar to output voltages)
        details.Add(new DemoDetailItem
        {
            Key = "loadPhaseAVoltage",
            Value = Math.Round(phaseAVoltage, 1),
            Unit = "V",
            LabelKey = "realtime.loadPhaseAVoltage"
        });

        details.Add(new DemoDetailItem
        {
            Key = "loadPhaseBVoltage",
            Value = Math.Round(phaseBVoltage, 1),
            Unit = "V",
            LabelKey = "realtime.loadPhaseBVoltage"
        });

        details.Add(new DemoDetailItem
        {
            Key = "loadPhaseCVoltage",
            Value = Math.Round(phaseCVoltage, 1),
            Unit = "V",
            LabelKey = "realtime.loadPhaseCVoltage"
        });

        // Load Frequency
        details.Add(new DemoDetailItem
        {
            Key = "loadFrequency",
            Value = Math.Round(outputFrequency, 2),
            Unit = "Hz",
            LabelKey = "realtime.loadFrequency"
        });

        // Load Scope
        details.Add(new DemoDetailItem
        {
            Key = "loadScope",
            Value = "backup_load",
            Unit = null,
            LabelKey = "realtime.loadScope"
        });

        // Inverter Heatsink Temperature (realistic 35-55°C during operation)
        var heatsinkTemp = 40.0 + Math.Abs(powerFlow.LoadPowerKw) * 3 + (voltageRandom.NextDouble() * 5);
        details.Add(new DemoDetailItem
        {
            Key = "inverterHeatsinkTemp",
            Value = Math.Round(heatsinkTemp, 1),
            Unit = "°C",
            LabelKey = "realtime.inverterHeatsinkTemp"
        });

        // Internal Ambient Temperature
        var ambientTemp = 28.0 + (voltageRandom.NextDouble() * 4);
        details.Add(new DemoDetailItem
        {
            Key = "internalAmbientTemp",
            Value = Math.Round(ambientTemp, 1),
            Unit = "°C",
            LabelKey = "realtime.internalAmbientTemp"
        });

        // AC Input Status
        details.Add(new DemoDetailItem
        {
            Key = "gridSignalPresent",
            Value = true,
            Unit = null,
            LabelKey = "realtime.gridSignalPresent"
        });

        details.Add(new DemoDetailItem
        {
            Key = "generatorSignalPresent",
            Value = false,
            Unit = null,
            LabelKey = "realtime.generatorSignalPresent"
        });

        return details;
    }

    public List<DemoDetailItem> GetDemoInverterParameterDetails(string? name, string? manufacturer, string? model, string? serialNumber)
    {
        var details = new List<DemoDetailItem>
        {
            new DemoDetailItem
            {
                Key = "name",
                Value = name ?? "Demo Inverter",
                Unit = null,
                LabelKey = "parameter.name"
            },
            new DemoDetailItem
            {
                Key = "manufacturer",
                Value = manufacturer ?? "Growatt",
                Unit = null,
                LabelKey = "parameter.manufacturer"
            },
            new DemoDetailItem
            {
                Key = "model",
                Value = model ?? "SPH5000",
                Unit = null,
                LabelKey = "parameter.model"
            },
            new DemoDetailItem
            {
                Key = "serialNumber",
                Value = serialNumber ?? "DEMO-INV-001",
                Unit = null,
                LabelKey = "parameter.serialNumber"
            },
            new DemoDetailItem
            {
                Key = "ratedPower",
                Value = 5000,
                Unit = "W",
                LabelKey = "parameter.ratedPower"
            },
            new DemoDetailItem
            {
                Key = "ratedVoltage",
                Value = 230,
                Unit = "V",
                LabelKey = "parameter.ratedVoltage"
            },
            new DemoDetailItem
            {
                Key = "ratedFrequency",
                Value = 50,
                Unit = "Hz",
                LabelKey = "parameter.ratedFrequency"
            },
            new DemoDetailItem
            {
                Key = "firmwareVersion",
                Value = "V3.2.1",
                Unit = null,
                LabelKey = "parameter.firmwareVersion"
            }
        };

        return details;
    }

    #endregion

    #region Additional Demo Data Methods

    public DemoDailyEnergySummary GetDemoDailyEnergySummary(Guid deviceId)
    {
        var now = DateTime.UtcNow;
        var hour = now.Hour;
        var random = new Random(now.DayOfYear); // Consistent for the day

        // Scale factors based on time of day (energy accumulates throughout the day)
        var dayProgress = hour / 24.0;

        // Base daily values for a 5kW system
        var dailyProduction = 32.5 * GetSeasonalPvMultiplier(now.Month);
        var dailyConsumption = 18.0 * GetSeasonalLoadMultiplier(now.Month);

        // Calculate current values based on time of day
        // Production follows bell curve (peak at midday)
        var productionProgress = hour < 6 ? 0 :
            (hour > 20 ? 1.0 : Math.Sin((hour - 6) * Math.PI / 14));

        var currentProduction = dailyProduction * productionProgress * (0.9 + random.NextDouble() * 0.2);
        var currentConsumption = dailyConsumption * dayProgress * (0.9 + random.NextDouble() * 0.2);

        // Grid import/export based on net energy
        var netEnergy = currentProduction - currentConsumption;
        var gridExport = netEnergy > 0 ? netEnergy * 0.6 : 0;
        var gridImport = netEnergy < 0 ? Math.Abs(netEnergy) * 0.7 : 0;

        return new DemoDailyEnergySummary
        {
            DeviceLocalTime = now,
            Production = Math.Round(currentProduction, 2),
            Consumption = Math.Round(currentConsumption, 2),
            GridExport = Math.Round(gridExport, 2),
            GridImport = Math.Round(gridImport, 2)
        };
    }

    public DemoSocResponse GetDemoSocResponse(Guid deviceId)
    {
        var hour = DateTime.UtcNow.Hour;
        var random = new Random(DateTime.UtcNow.Minute); // Slight variation each minute

        // SOC pattern based on time of day
        double baseSoc = hour switch
        {
            >= 0 and < 6 => 45 + random.NextDouble() * 10,    // Night: 45-55%
            >= 6 and < 9 => 35 + random.NextDouble() * 10,    // Morning: 35-45%
            >= 9 and < 12 => 55 + random.NextDouble() * 20,   // Late morning: 55-75%
            >= 12 and < 15 => 80 + random.NextDouble() * 15,  // Midday: 80-95%
            >= 15 and < 18 => 85 + random.NextDouble() * 15,  // Afternoon: 85-100%
            >= 18 and < 21 => 60 + random.NextDouble() * 20,  // Evening: 60-80%
            _ => 50 + random.NextDouble() * 15                 // Late evening: 50-65%
        };

        return new DemoSocResponse
        {
            TimestampUtc = DateTime.UtcNow.ToString("o"),
            Version = 1,
            InverterBatChannelSoc = new List<DemoInverterBatChannelSoc>
            {
                new DemoInverterBatChannelSoc { ChannelId = 1, SocPercent = Math.Round(baseSoc, 1) }
            },
            BmsSocDetails = new List<DemoBmsSocDetail>
            {
                new DemoBmsSocDetail
                {
                    ChannelId = 1,
                    DeviceId = "DEMO-BMS-001",
                    SocPercent = Math.Round(baseSoc, 1)
                }
            }
        };
    }

    public DemoDeviceModeResult GetDemoDeviceMode()
    {
        return new DemoDeviceModeResult
        {
            Mode = "SelfConsumption",
            ModeDescription = "Self Consumption",
            LastModified = DateTimeOffset.UtcNow.AddDays(-30) // Simulate mode was set 30 days ago
        };
    }

    public DemoLifetimeEnergy GetDemoLifetimeEnergy()
    {
        var now = DateTime.UtcNow;

        // Simulate a system installed ~2 years ago
        // Average daily production: ~25 kWh, consumption: ~18 kWh
        // Days since "installation": ~730 days (2 years)
        var daysInstalled = 730 + now.DayOfYear;

        // Total lifetime energy calculations
        // Grid Import: Energy bought from grid when solar insufficient
        // Grid Export: Excess solar energy sold to grid
        var baseGridImport = 3500.0; // kWh over 2 years (low due to good solar)
        var baseGridExport = 8200.0; // kWh exported over 2 years

        // Add some variation based on current date
        var dayVariation = now.DayOfYear * 2.5; // ~2.5 kWh variation per day

        return new DemoLifetimeEnergy
        {
            Timestamp = now,
            GridImport = Math.Round(baseGridImport + dayVariation * 0.4, 2),
            GridExport = Math.Round(baseGridExport + dayVariation * 0.8, 2)
        };
    }

    public DemoHeartbeatStatus GetDemoHeartbeatStatus(Guid deviceId)
    {
        // Demo devices are always online with OK status
        return new DemoHeartbeatStatus
        {
            Status = "OK",
            Timestamp = DateTime.UtcNow
        };
    }

    #endregion
}

#region Data Transfer Objects

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
