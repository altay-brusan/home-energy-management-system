using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.DeviceComponents.Queries.GetDeviceDetails;

public class GetDeviceDetailsQueryHandler : IRequestHandler<GetDeviceDetailsQuery, DeviceDetailsResponseDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IThingsBoardConnectorService _tbService;
    private readonly IDeviceMappingService _deviceMapping;
    private readonly ILogger<GetDeviceDetailsQueryHandler> _logger;
    private readonly IDemoDataService _demoDataService;
    private readonly IDemoSimulationDataService _demoSimulationService;

    public GetDeviceDetailsQueryHandler(
        IApplicationDbContext context,
        IThingsBoardConnectorService tbService,
        IDeviceMappingService deviceMapping,
        ILogger<GetDeviceDetailsQueryHandler> logger,
        IDemoDataService demoDataService,
        IDemoSimulationDataService demoSimulationService)
    {
        _context = context;
        _tbService = tbService;
        _deviceMapping = deviceMapping;
        _logger = logger;
        _demoDataService = demoDataService;
        _demoSimulationService = demoSimulationService;
    }

    public async Task<DeviceDetailsResponseDto?> Handle(GetDeviceDetailsQuery request, CancellationToken cancellationToken)
    {
        // Step 1: Get DeviceComponent by ID with navigation to Device
        var component = await _context.DeviceComponents
            .Include(c => c.Device)
            .FirstOrDefaultAsync(c => c.Id == request.ComponentId && !c.IsDeleted, cancellationToken);

        if (component == null)
        {
            _logger.LogWarning("Device component {ComponentId} not found", request.ComponentId);
            return null;
        }

        // Step 2: Check if this is a demo device - return simulated data
        var isDemoDevice = await _demoDataService.IsDemoDeviceAsync(component.DeviceId, cancellationToken);
        if (isDemoDevice)
        {
            return BuildDemoDeviceDetails(component);
        }

        // Step 3: Get ThingsBoard Device ID for real devices
        var tbDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(component.DeviceId, cancellationToken);
        if (tbDeviceId == null)
        {
            _logger.LogWarning("ThingsBoard device mapping not found for device {DeviceId}", component.DeviceId);
            return null;
        }

        // Step 4: Call appropriate RPC based on device type
        var response = new DeviceDetailsResponseDto();

        switch (component.DeviceType)
        {
            case DeviceType.Invertor:
                response.Details = await BuildInverterDetailsAsync(component, tbDeviceId.Value, cancellationToken);
                break;

            case DeviceType.BMS:
            case DeviceType.Battery:
                response.Details = await BuildBatteryDetailsAsync(component, tbDeviceId.Value, cancellationToken);
                break;

            default:
                _logger.LogWarning("Unsupported device type {DeviceType} for component {ComponentId}",
                    component.DeviceType, component.Id);
                return null;
        }

        return response;
    }

    #region Demo Device Details

    /// <summary>
    /// Build device details for demo devices using simulated data
    /// </summary>
    private DeviceDetailsResponseDto BuildDemoDeviceDetails(Domain.Entities.DeviceComponent component)
    {
        var response = new DeviceDetailsResponseDto();

        switch (component.DeviceType)
        {
            case DeviceType.Invertor:
                response.Details = BuildDemoInverterDetails(component);
                break;

            case DeviceType.BMS:
            case DeviceType.Battery:
                response.Details = BuildDemoBatteryDetails(component);
                break;

            default:
                _logger.LogWarning("Unsupported device type {DeviceType} for demo component {ComponentId}",
                    component.DeviceType, component.Id);
                response.Details = new List<DeviceDetailItemDto>();
                break;
        }

        return response;
    }

    /// <summary>
    /// Build simulated battery details for demo devices
    /// Uses GetBatterySoc() from simulation service for consistent SOC with dashboard
    /// </summary>
    private List<DeviceDetailItemDto> BuildDemoBatteryDetails(Domain.Entities.DeviceComponent component)
    {
        var details = new List<DeviceDetailItemDto>();

        // Get SOC from simulation service (same as dashboard uses)
        var soc = _demoSimulationService.GetBatterySoc(DateTime.UtcNow);

        // Realtime data
        details.Add(new DeviceDetailItemDto
        {
            Key = "soc",
            Value = Math.Round(soc, 1),
            Unit = "%",
            LabelKey = "realtime.soc"
        });

        // State of Health (simulated - batteries typically maintain 95%+ for first few years)
        details.Add(new DeviceDetailItemDto
        {
            Key = "soh",
            Value = 98.5,
            Unit = "%",
            LabelKey = "realtime.soh"
        });

        // Battery voltage - realistic for a 51.2V LiFePO4 battery pack
        // Voltage varies with SOC: ~48V at 0%, ~54V at 100%
        var voltage = 48.0 + (soc / 100.0) * 6.0;
        details.Add(new DeviceDetailItemDto
        {
            Key = "voltage",
            Value = Math.Round(voltage, 1),
            Unit = "V",
            LabelKey = "realtime.voltage"
        });

        // Battery current - based on current power flow
        // Positive = charging, Negative = discharging
        var powerFlow = _demoSimulationService.GetCurrentPowerFlow(Guid.Empty);
        var batteryCurrent = (powerFlow.BatteryPowerKw * 1000) / voltage; // I = P/V
        details.Add(new DeviceDetailItemDto
        {
            Key = "current",
            Value = Math.Round(batteryCurrent, 2),
            Unit = "A",
            LabelKey = "realtime.current"
        });

        // Battery temperature (realistic range 20-35°C during operation)
        var hour = DateTime.UtcNow.Hour;
        var baseTemp = 25.0 + Math.Sin((hour - 6) * Math.PI / 12) * 5; // Varies with time of day
        details.Add(new DeviceDetailItemDto
        {
            Key = "temperature",
            Value = Math.Round(baseTemp, 1),
            Unit = "°C",
            LabelKey = "realtime.temperature"
        });

        // Cycle count (simulated - ~500 cycles for a 2-year old battery)
        details.Add(new DeviceDetailItemDto
        {
            Key = "cycleCount",
            Value = 487,
            Unit = null,
            LabelKey = "realtime.cycleCount"
        });

        // Parameter data from component
        AddDemoBatteryParameterDetails(details, component);

        return details;
    }

    /// <summary>
    /// Add parameter details for demo battery
    /// </summary>
    private void AddDemoBatteryParameterDetails(List<DeviceDetailItemDto> details, Domain.Entities.DeviceComponent component)
    {
        // Name
        details.Add(new DeviceDetailItemDto
        {
            Key = "name",
            Value = component.Name ?? "Demo Battery",
            Unit = null,
            LabelKey = "parameter.name"
        });

        // Manufacturer
        details.Add(new DeviceDetailItemDto
        {
            Key = "manufacturer",
            Value = component.Manufacturer ?? "Pylontech",
            Unit = null,
            LabelKey = "parameter.manufacturer"
        });

        // Model
        details.Add(new DeviceDetailItemDto
        {
            Key = "model",
            Value = component.ModelNumber ?? "US3000C",
            Unit = null,
            LabelKey = "parameter.model"
        });

        // Serial Number
        details.Add(new DeviceDetailItemDto
        {
            Key = "serialNumber",
            Value = component.SerialNumber ?? "DEMO-BAT-001",
            Unit = null,
            LabelKey = "parameter.serialNumber"
        });

        // Capacity
        details.Add(new DeviceDetailItemDto
        {
            Key = "capacity",
            Value = 3.55,
            Unit = "kWh",
            LabelKey = "parameter.capacity"
        });

        // Rated Voltage
        details.Add(new DeviceDetailItemDto
        {
            Key = "ratedVoltage",
            Value = 51.2,
            Unit = "V",
            LabelKey = "parameter.ratedVoltage"
        });

        // Chemistry
        details.Add(new DeviceDetailItemDto
        {
            Key = "chemistry",
            Value = "LiFePO4",
            Unit = null,
            LabelKey = "parameter.chemistry"
        });

        // Firmware Version
        details.Add(new DeviceDetailItemDto
        {
            Key = "firmwareVersion",
            Value = "V2.8",
            Unit = null,
            LabelKey = "parameter.firmwareVersion"
        });
    }

    /// <summary>
    /// Build simulated inverter details for demo devices
    /// </summary>
    private List<DeviceDetailItemDto> BuildDemoInverterDetails(Domain.Entities.DeviceComponent component)
    {
        var details = new List<DeviceDetailItemDto>();

        // Get current power flow for realistic values
        var powerFlow = _demoSimulationService.GetCurrentPowerFlow(Guid.Empty);

        // Run State
        var hour = DateTime.UtcNow.Hour;
        var runState = (powerFlow.SolarPowerKw > 0.1) ? "Normal Operation" : "Standby";
        details.Add(new DeviceDetailItemDto
        {
            Key = "runState",
            Value = runState,
            Unit = null,
            LabelKey = "realtime.runState"
        });

        // Output Active Power (total load power in W)
        details.Add(new DeviceDetailItemDto
        {
            Key = "outputActivePower",
            Value = Math.Round(powerFlow.LoadPowerKw * 1000, 0),
            Unit = "W",
            LabelKey = "realtime.outputActivePower"
        });

        // Output Apparent Power (slightly higher than active due to power factor)
        var apparentPower = powerFlow.LoadPowerKw * 1000 * 1.05;
        details.Add(new DeviceDetailItemDto
        {
            Key = "outputApparentPower",
            Value = Math.Round(apparentPower, 0),
            Unit = "VA",
            LabelKey = "realtime.outputApparentPower"
        });

        // Output Voltage (European standard 230V ± 5%)
        var outputVoltage = 230.0 + (new Random().NextDouble() * 10 - 5);
        details.Add(new DeviceDetailItemDto
        {
            Key = "outputVoltage",
            Value = Math.Round(outputVoltage, 1),
            Unit = "V",
            LabelKey = "realtime.outputVoltage"
        });

        // Output Current (I = P / V)
        var outputCurrent = (powerFlow.LoadPowerKw * 1000) / outputVoltage;
        details.Add(new DeviceDetailItemDto
        {
            Key = "outputCurrent",
            Value = Math.Round(outputCurrent, 2),
            Unit = "A",
            LabelKey = "realtime.outputCurrent"
        });

        // Output Frequency (European standard 50Hz)
        var outputFrequency = 50.0 + (new Random().NextDouble() * 0.2 - 0.1);
        details.Add(new DeviceDetailItemDto
        {
            Key = "outputFrequency",
            Value = Math.Round(outputFrequency, 2),
            Unit = "Hz",
            LabelKey = "realtime.outputFrequency"
        });

        // Grid Side Active Power
        details.Add(new DeviceDetailItemDto
        {
            Key = "gridSideActivePower",
            Value = Math.Round(powerFlow.GridPowerKw * 1000, 0),
            Unit = "W",
            LabelKey = "realtime.gridSideActivePower"
        });

        // Inverter Heatsink Temperature (realistic 35-55°C during operation)
        var heatsinkTemp = 40.0 + Math.Abs(powerFlow.LoadPowerKw) * 3 + (new Random().NextDouble() * 5);
        details.Add(new DeviceDetailItemDto
        {
            Key = "inverterHeatsinkTemp",
            Value = Math.Round(heatsinkTemp, 1),
            Unit = "°C",
            LabelKey = "realtime.inverterHeatsinkTemp"
        });

        // Internal Ambient Temperature
        var ambientTemp = 28.0 + (new Random().NextDouble() * 4);
        details.Add(new DeviceDetailItemDto
        {
            Key = "internalAmbientTemp",
            Value = Math.Round(ambientTemp, 1),
            Unit = "°C",
            LabelKey = "realtime.internalAmbientTemp"
        });

        // AC Input Status
        details.Add(new DeviceDetailItemDto
        {
            Key = "gridSignalPresent",
            Value = true,
            Unit = null,
            LabelKey = "realtime.gridSignalPresent"
        });

        details.Add(new DeviceDetailItemDto
        {
            Key = "generatorSignalPresent",
            Value = false,
            Unit = null,
            LabelKey = "realtime.generatorSignalPresent"
        });

        // Parameter data from component
        AddDemoInverterParameterDetails(details, component);

        return details;
    }

    /// <summary>
    /// Add parameter details for demo inverter
    /// </summary>
    private void AddDemoInverterParameterDetails(List<DeviceDetailItemDto> details, Domain.Entities.DeviceComponent component)
    {
        // Name
        details.Add(new DeviceDetailItemDto
        {
            Key = "name",
            Value = component.Name ?? "Demo Inverter",
            Unit = null,
            LabelKey = "parameter.name"
        });

        // Manufacturer
        details.Add(new DeviceDetailItemDto
        {
            Key = "manufacturer",
            Value = component.Manufacturer ?? "Growatt",
            Unit = null,
            LabelKey = "parameter.manufacturer"
        });

        // Model
        details.Add(new DeviceDetailItemDto
        {
            Key = "model",
            Value = component.ModelNumber ?? "SPH5000",
            Unit = null,
            LabelKey = "parameter.model"
        });

        // Serial Number
        details.Add(new DeviceDetailItemDto
        {
            Key = "serialNumber",
            Value = component.SerialNumber ?? "DEMO-INV-001",
            Unit = null,
            LabelKey = "parameter.serialNumber"
        });

        // Rated Power
        details.Add(new DeviceDetailItemDto
        {
            Key = "ratedPower",
            Value = 5000,
            Unit = "W",
            LabelKey = "parameter.ratedPower"
        });

        // Rated Voltage
        details.Add(new DeviceDetailItemDto
        {
            Key = "ratedVoltage",
            Value = 230,
            Unit = "V",
            LabelKey = "parameter.ratedVoltage"
        });

        // Rated Frequency
        details.Add(new DeviceDetailItemDto
        {
            Key = "ratedFrequency",
            Value = 50,
            Unit = "Hz",
            LabelKey = "parameter.ratedFrequency"
        });

        // Firmware Version
        details.Add(new DeviceDetailItemDto
        {
            Key = "firmwareVersion",
            Value = "V3.2.1",
            Unit = null,
            LabelKey = "parameter.firmwareVersion"
        });
    }

    #endregion

    #region Inverter Details

    private async Task<List<DeviceDetailItemDto>> BuildInverterDetailsAsync(
        Domain.Entities.DeviceComponent component,
        Guid tbDeviceId,
        CancellationToken cancellationToken)
    {
        var details = new List<DeviceDetailItemDto>();

        // Get inverter state from RPC
        var state = await GetInverterDeviceStateFromRpcAsync(tbDeviceId, cancellationToken);

        if (state != null)
        {
            // Add realtime data
            AddInverterRealtimeDetails(details, state);
        }

        // Add parameter data from database
        AddInverterParameterDetails(details, component);

        return details;
    }

    private void AddInverterRealtimeDetails(List<DeviceDetailItemDto> details, InverterDeviceStateDto state)
    {
        // Run State
        details.Add(new DeviceDetailItemDto
        {
            Key = "runState",
            Value = state.RunState,
            Unit = null,
            LabelKey = "realtime.runState"
        });

        // Output to AC bus
        if (state.OutputActivePowerW.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "outputActivePower",
                Value = state.OutputActivePowerW.Value,
                Unit = "W",
                LabelKey = "realtime.outputActivePower"
            });
        }

        if (state.OutputApparentPowerVA.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "outputApparentPower",
                Value = state.OutputApparentPowerVA.Value,
                Unit = "VA",
                LabelKey = "realtime.outputApparentPower"
            });
        }

        if (state.OutputVoltageV.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "outputVoltage",
                Value = state.OutputVoltageV.Value,
                Unit = "V",
                LabelKey = "realtime.outputVoltage"
            });
        }

        if (state.OutputCurrentA.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "outputCurrent",
                Value = state.OutputCurrentA.Value,
                Unit = "A",
                LabelKey = "realtime.outputCurrent"
            });
        }

        if (state.OutputFrequencyHz.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "outputFrequency",
                Value = state.OutputFrequencyHz.Value,
                Unit = "Hz",
                LabelKey = "realtime.outputFrequency"
            });
        }

        // Grid-side power
        if (state.GridSideActivePowerW.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "gridSideActivePower",
                Value = state.GridSideActivePowerW.Value,
                Unit = "W",
                LabelKey = "realtime.gridSideActivePower"
            });
        }

        if (state.GridSidePhaseAPowerW.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "gridSidePhaseAPower",
                Value = state.GridSidePhaseAPowerW.Value,
                Unit = "W",
                LabelKey = "realtime.gridSidePhaseAPower"
            });
        }

        if (state.GridSidePhaseBPowerW.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "gridSidePhaseBPower",
                Value = state.GridSidePhaseBPowerW.Value,
                Unit = "W",
                LabelKey = "realtime.gridSidePhaseBPower"
            });
        }

        if (state.GridSidePhaseCPowerW.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "gridSidePhaseCPower",
                Value = state.GridSidePhaseCPowerW.Value,
                Unit = "W",
                LabelKey = "realtime.gridSidePhaseCPower"
            });
        }

        // Temperatures
        if (state.InverterHeatsinkTempC.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "inverterHeatsinkTemp",
                Value = state.InverterHeatsinkTempC.Value,
                Unit = "°C",
                LabelKey = "realtime.inverterHeatsinkTemp"
            });
        }

        if (state.InternalAmbientTempC.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "internalAmbientTemp",
                Value = state.InternalAmbientTempC.Value,
                Unit = "°C",
                LabelKey = "realtime.internalAmbientTemp"
            });
        }

        // AC Input Status
        if (state.AcInputStatus != null)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "gridSignalPresent",
                Value = state.AcInputStatus.GridSignalPresent,
                Unit = null,
                LabelKey = "realtime.gridSignalPresent"
            });

            details.Add(new DeviceDetailItemDto
            {
                Key = "generatorSignalPresent",
                Value = state.AcInputStatus.GeneratorSignalPresent,
                Unit = null,
                LabelKey = "realtime.generatorSignalPresent"
            });
        }
    }

    private void AddInverterParameterDetails(List<DeviceDetailItemDto> details, Domain.Entities.DeviceComponent component)
    {
        // Name
        details.Add(new DeviceDetailItemDto
        {
            Key = "name",
            Value = component.Name,
            Unit = null,
            LabelKey = "parameter.name"
        });

        // Manufacturer
        if (!string.IsNullOrEmpty(component.Manufacturer))
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "manufacturer",
                Value = component.Manufacturer,
                Unit = null,
                LabelKey = "parameter.manufacturer"
            });
        }

        // Model Number
        if (!string.IsNullOrEmpty(component.ModelNumber))
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "model",
                Value = component.ModelNumber,
                Unit = null,
                LabelKey = "parameter.model"
            });
        }

        // Serial Number
        if (!string.IsNullOrEmpty(component.SerialNumber))
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "serialNumber",
                Value = component.SerialNumber,
                Unit = null,
                LabelKey = "parameter.serialNumber"
            });
        }

        // Parse specifications JSON for additional parameters
        if (component.Specifications != null)
        {
            try
            {
                var specs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(component.Specifications);
                if (specs != null)
                {
                    // Rated Power
                    if (specs.TryGetValue("ratedPowerW", out var ratedPower))
                    {
                        details.Add(new DeviceDetailItemDto
                        {
                            Key = "ratedPower",
                            Value = ratedPower.GetDouble(),
                            Unit = "W",
                            LabelKey = "parameter.ratedPower"
                        });
                    }

                    // Firmware Version
                    if (specs.TryGetValue("firmwareVersion", out var firmware))
                    {
                        details.Add(new DeviceDetailItemDto
                        {
                            Key = "firmwareVersion",
                            Value = firmware.GetString(),
                            Unit = null,
                            LabelKey = "parameter.firmwareVersion"
                        });
                    }

                    // Rated Voltage
                    if (specs.TryGetValue("ratedVoltageV", out var ratedVoltage))
                    {
                        details.Add(new DeviceDetailItemDto
                        {
                            Key = "ratedVoltage",
                            Value = ratedVoltage.GetDouble(),
                            Unit = "V",
                            LabelKey = "parameter.ratedVoltage"
                        });
                    }

                    // Rated Frequency
                    if (specs.TryGetValue("ratedFrequencyHz", out var ratedFrequency))
                    {
                        details.Add(new DeviceDetailItemDto
                        {
                            Key = "ratedFrequency",
                            Value = ratedFrequency.GetDouble(),
                            Unit = "Hz",
                            LabelKey = "parameter.ratedFrequency"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse specifications for component {ComponentId}", component.Id);
            }
        }
    }

    private async Task<InverterDeviceStateDto?> GetInverterDeviceStateFromRpcAsync(
        Guid tbDeviceId,
        CancellationToken cancellationToken)
    {
        try
        {
            var rpcResponse = await _tbService.SendTwoWayRpcAsync(
                tbDeviceId,
                "getInverterDeviceState",
                new Dictionary<string, object>(),
                timeoutMs: 10000,
                cancellationToken: cancellationToken);

            if (rpcResponse?.Success == true && rpcResponse.Response != null)
            {
                // Response is wrapped: {"success": true, "data": {...}}
                // Extract the "data" property if present
                if (rpcResponse.Response.TryGetValue("data", out var dataObj) && dataObj != null)
                {
                    var dataJson = JsonSerializer.Serialize(dataObj);
                    return JsonSerializer.Deserialize<InverterDeviceStateDto>(dataJson);
                }

                // Fallback: try deserializing the whole response
                var json = JsonSerializer.Serialize(rpcResponse.Response);
                return JsonSerializer.Deserialize<InverterDeviceStateDto>(json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get inverter device state from RPC for device {DeviceId}", tbDeviceId);
        }

        return null;
    }

    #endregion

    #region Battery Details

    private async Task<List<DeviceDetailItemDto>> BuildBatteryDetailsAsync(
        Domain.Entities.DeviceComponent component,
        Guid tbDeviceId,
        CancellationToken cancellationToken)
    {
        var details = new List<DeviceDetailItemDto>();

        // Get battery SoC state from RPC
        var socState = await GetBatterySocStateFromRpcAsync(tbDeviceId, cancellationToken);

        if (socState != null)
        {
            // Add realtime data (SoC, SoH)
            AddBatteryRealtimeDetails(details, socState, component.SerialNumber);
        }

        // Add parameter data from database
        AddBatteryParameterDetails(details, component);

        return details;
    }

    private void AddBatteryRealtimeDetails(List<DeviceDetailItemDto> details, BatterySocStateDto socState, string? componentSerialNumber)
    {
        // Try to find matching battery data by serial number
        BmsSocDetailRpcDto? matchingBattery = null;

        if (socState.BmsSocDetails != null && !string.IsNullOrEmpty(componentSerialNumber))
        {
            matchingBattery = socState.BmsSocDetails
                .FirstOrDefault(b => b.DeviceId == componentSerialNumber);
        }

        // If we found a matching battery, use its data
        if (matchingBattery != null)
        {
            if (matchingBattery.SocPercent.HasValue)
            {
                details.Add(new DeviceDetailItemDto
                {
                    Key = "soc",
                    Value = matchingBattery.SocPercent.Value,
                    Unit = "%",
                    LabelKey = "realtime.soc"
                });
            }

            // Add SoH if available (you might need to add this to BmsSocDetailRpcDto)
            // For now, we'll skip it if not available
        }
        else if (socState.InverterBatChannelSoc != null && socState.InverterBatChannelSoc.Any())
        {
            // Fall back to inverter-level battery data (average SoC)
            var avgSoc = socState.InverterBatChannelSoc
                .Where(c => c.SocPercent.HasValue)
                .Average(c => c.SocPercent!.Value);

            details.Add(new DeviceDetailItemDto
            {
                Key = "soc",
                Value = Math.Round(avgSoc, 1),
                Unit = "%",
                LabelKey = "realtime.soc"
            });
        }
    }

    private void AddBatteryParameterDetails(List<DeviceDetailItemDto> details, Domain.Entities.DeviceComponent component)
    {
        // Name
        details.Add(new DeviceDetailItemDto
        {
            Key = "name",
            Value = component.Name,
            Unit = null,
            LabelKey = "parameter.name"
        });

        // Manufacturer
        if (!string.IsNullOrEmpty(component.Manufacturer))
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "manufacturer",
                Value = component.Manufacturer,
                Unit = null,
                LabelKey = "parameter.manufacturer"
            });
        }

        // Model Number
        if (!string.IsNullOrEmpty(component.ModelNumber))
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "model",
                Value = component.ModelNumber,
                Unit = null,
                LabelKey = "parameter.model"
            });
        }

        // Serial Number
        if (!string.IsNullOrEmpty(component.SerialNumber))
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "serialNumber",
                Value = component.SerialNumber,
                Unit = null,
                LabelKey = "parameter.serialNumber"
            });
        }

        // Parse specifications JSON for additional parameters
        if (component.Specifications != null)
        {
            try
            {
                var specs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(component.Specifications);
                if (specs != null)
                {
                    // Capacity
                    if (specs.TryGetValue("capacityKWh", out var capacity))
                    {
                        details.Add(new DeviceDetailItemDto
                        {
                            Key = "capacity",
                            Value = capacity.GetDouble(),
                            Unit = "kWh",
                            LabelKey = "parameter.capacity"
                        });
                    }

                    // Rated Voltage
                    if (specs.TryGetValue("ratedVoltageV", out var ratedVoltage))
                    {
                        details.Add(new DeviceDetailItemDto
                        {
                            Key = "ratedVoltage",
                            Value = ratedVoltage.GetDouble(),
                            Unit = "V",
                            LabelKey = "parameter.ratedVoltage"
                        });
                    }

                    // Chemistry Type
                    if (specs.TryGetValue("chemistry", out var chemistry))
                    {
                        details.Add(new DeviceDetailItemDto
                        {
                            Key = "chemistry",
                            Value = chemistry.GetString(),
                            Unit = null,
                            LabelKey = "parameter.chemistry"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse specifications for component {ComponentId}", component.Id);
            }
        }
    }

    private async Task<BatterySocStateDto?> GetBatterySocStateFromRpcAsync(
        Guid tbDeviceId,
        CancellationToken cancellationToken)
    {
        try
        {
            var rpcResponse = await _tbService.SendTwoWayRpcAsync(
                tbDeviceId,
                "getBatterySocState",
                new Dictionary<string, object>(),
                timeoutMs: 10000,
                cancellationToken: cancellationToken);

            if (rpcResponse?.Success == true && rpcResponse.Response != null)
            {
                var json = JsonSerializer.Serialize(rpcResponse.Response);
                return JsonSerializer.Deserialize<BatterySocStateDto>(json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get battery SoC state from RPC for device {DeviceId}", tbDeviceId);
        }

        return null;
    }

    #endregion
}
