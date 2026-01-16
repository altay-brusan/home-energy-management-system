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
    private readonly IDeviceActivityService _activityService;
    private readonly ILogger<GetDeviceDetailsQueryHandler> _logger;
    private readonly IDemoDataService _demoDataService;
    private readonly IDemoSimulationDataService _demoSimulationService;

    public GetDeviceDetailsQueryHandler(
        IApplicationDbContext context,
        IThingsBoardConnectorService tbService,
        IDeviceMappingService deviceMapping,
        IDeviceActivityService activityService,
        ILogger<GetDeviceDetailsQueryHandler> logger,
        IDemoDataService demoDataService,
        IDemoSimulationDataService demoSimulationService)
    {
        _context = context;
        _tbService = tbService;
        _deviceMapping = deviceMapping;
        _activityService = activityService;
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
    /// Build device details for demo devices using centralized DemoSimulationDataService
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
    /// Build simulated battery details for demo devices using centralized service
    /// </summary>
    private List<DeviceDetailItemDto> BuildDemoBatteryDetails(Domain.Entities.DeviceComponent component)
    {
        var details = new List<DeviceDetailItemDto>();

        // Get realtime data from centralized service
        var realtimeDetails = _demoSimulationService.GetDemoBatteryRealtimeDetails();
        details.AddRange(realtimeDetails.Select(d => new DeviceDetailItemDto
        {
            Key = d.Key,
            Value = d.Value,
            Unit = d.Unit,
            LabelKey = d.LabelKey
        }));

        // Get parameter data from centralized service
        var parameterDetails = _demoSimulationService.GetDemoBatteryParameterDetails(
            component.Name, component.Manufacturer, component.ModelNumber, component.SerialNumber);
        details.AddRange(parameterDetails.Select(d => new DeviceDetailItemDto
        {
            Key = d.Key,
            Value = d.Value,
            Unit = d.Unit,
            LabelKey = d.LabelKey
        }));

        return details;
    }

    /// <summary>
    /// Build simulated inverter details for demo devices using centralized service
    /// </summary>
    private List<DeviceDetailItemDto> BuildDemoInverterDetails(Domain.Entities.DeviceComponent component)
    {
        var details = new List<DeviceDetailItemDto>();

        // Get realtime data from centralized service
        var realtimeDetails = _demoSimulationService.GetDemoInverterRealtimeDetails();
        details.AddRange(realtimeDetails.Select(d => new DeviceDetailItemDto
        {
            Key = d.Key,
            Value = d.Value,
            Unit = d.Unit,
            LabelKey = d.LabelKey
        }));

        // Get parameter data from centralized service
        var parameterDetails = _demoSimulationService.GetDemoInverterParameterDetails(
            component.Name, component.Manufacturer, component.ModelNumber, component.SerialNumber);
        details.AddRange(parameterDetails.Select(d => new DeviceDetailItemDto
        {
            Key = d.Key,
            Value = d.Value,
            Unit = d.Unit,
            LabelKey = d.LabelKey
        }));

        return details;
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

        // Get load state from RPC
        var loadState = await GetLoadDeviceStateFromRpcAsync(tbDeviceId, cancellationToken);

        if (loadState != null)
        {
            // Add load realtime data
            AddLoadRealtimeDetails(details, loadState);
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

        // Three-phase output data (Phases array format expected by frontend)
        var phases = new List<Dictionary<string, object>>();

        // Phase A
        if (state.OutputPhaseAVoltageV.HasValue || state.OutputPhaseACurrentA.HasValue || state.OutputPhaseAPowerW.HasValue)
        {
            phases.Add(new Dictionary<string, object>
            {
                { "Phase Id", 1 },
                { "Voltage", state.OutputPhaseAVoltageV ?? 0 },
                { "Current", state.OutputPhaseACurrentA ?? 0 },
                { "Power", state.OutputPhaseAPowerW ?? 0 }
            });
        }

        // Phase B
        if (state.OutputPhaseBVoltageV.HasValue || state.OutputPhaseBCurrentA.HasValue || state.OutputPhaseBPowerW.HasValue)
        {
            phases.Add(new Dictionary<string, object>
            {
                { "Phase Id", 2 },
                { "Voltage", state.OutputPhaseBVoltageV ?? 0 },
                { "Current", state.OutputPhaseBCurrentA ?? 0 },
                { "Power", state.OutputPhaseBPowerW ?? 0 }
            });
        }

        // Phase C
        if (state.OutputPhaseCVoltageV.HasValue || state.OutputPhaseCCurrentA.HasValue || state.OutputPhaseCPowerW.HasValue)
        {
            phases.Add(new Dictionary<string, object>
            {
                { "Phase Id", 3 },
                { "Voltage", state.OutputPhaseCVoltageV ?? 0 },
                { "Current", state.OutputPhaseCCurrentA ?? 0 },
                { "Power", state.OutputPhaseCPowerW ?? 0 }
            });
        }

        // Add phases array if we have any phase data
        if (phases.Count > 0)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "Phases",
                Value = phases,
                Unit = null,
                LabelKey = "realtime.phases"
            });
        }

        // Output Frequency - single value shared across all phases
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
                // Record device activity - device is online since RPC succeeded
                await _activityService.RecordActivityAsync(
                    tbDeviceId,
                    TelemetrySource.PowerFlow,
                    cancellationToken: cancellationToken);

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

    private async Task<LoadDeviceStateDto?> GetLoadDeviceStateFromRpcAsync(
        Guid tbDeviceId,
        CancellationToken cancellationToken)
    {
        try
        {
            var rpcResponse = await _tbService.SendTwoWayRpcAsync(
                tbDeviceId,
                "getLoadDeviceState",
                new Dictionary<string, object>(),
                timeoutMs: 10000,
                cancellationToken: cancellationToken);

            if (rpcResponse?.Success == true && rpcResponse.Response != null)
            {
                // Record device activity - device is online since RPC succeeded
                await _activityService.RecordActivityAsync(
                    tbDeviceId,
                    TelemetrySource.PowerFlow,
                    cancellationToken: cancellationToken);

                // Response is wrapped: {"success": true, "data": {...}}
                // Extract the "data" property if present
                if (rpcResponse.Response.TryGetValue("data", out var dataObj) && dataObj != null)
                {
                    var dataJson = JsonSerializer.Serialize(dataObj);
                    return JsonSerializer.Deserialize<LoadDeviceStateDto>(dataJson);
                }

                // Fallback: try deserializing the whole response
                var json = JsonSerializer.Serialize(rpcResponse.Response);
                return JsonSerializer.Deserialize<LoadDeviceStateDto>(json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get load device state from RPC for device {DeviceId}", tbDeviceId);
        }

        return null;
    }

    private void AddLoadRealtimeDetails(List<DeviceDetailItemDto> details, LoadDeviceStateDto state)
    {
        // Load Power - Total
        if (state.LoadTotalActivePowerW.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "loadTotalActivePower",
                Value = state.LoadTotalActivePowerW.Value,
                Unit = "W",
                LabelKey = "realtime.loadTotalActivePower"
            });
        }

        // Load Power - Phase A
        if (state.LoadPhaseAPowerW.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "loadPhaseAPower",
                Value = state.LoadPhaseAPowerW.Value,
                Unit = "W",
                LabelKey = "realtime.loadPhaseAPower"
            });
        }

        // Load Power - Phase B
        if (state.LoadPhaseBPowerW.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "loadPhaseBPower",
                Value = state.LoadPhaseBPowerW.Value,
                Unit = "W",
                LabelKey = "realtime.loadPhaseBPower"
            });
        }

        // Load Power - Phase C
        if (state.LoadPhaseCPowerW.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "loadPhaseCPower",
                Value = state.LoadPhaseCPowerW.Value,
                Unit = "W",
                LabelKey = "realtime.loadPhaseCPower"
            });
        }

        // Load Voltage - Phase A
        if (state.LoadPhaseAVoltageV.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "loadPhaseAVoltage",
                Value = state.LoadPhaseAVoltageV.Value,
                Unit = "V",
                LabelKey = "realtime.loadPhaseAVoltage"
            });
        }

        // Load Voltage - Phase B
        if (state.LoadPhaseBVoltageV.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "loadPhaseBVoltage",
                Value = state.LoadPhaseBVoltageV.Value,
                Unit = "V",
                LabelKey = "realtime.loadPhaseBVoltage"
            });
        }

        // Load Voltage - Phase C
        if (state.LoadPhaseCVoltageV.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "loadPhaseCVoltage",
                Value = state.LoadPhaseCVoltageV.Value,
                Unit = "V",
                LabelKey = "realtime.loadPhaseCVoltage"
            });
        }

        // Load Frequency
        if (state.FrequencyHz.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "loadFrequency",
                Value = state.FrequencyHz.Value,
                Unit = "Hz",
                LabelKey = "realtime.loadFrequency"
            });
        }

        // Load Scope (backup_load or total_load)
        if (!string.IsNullOrEmpty(state.Scope))
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "loadScope",
                Value = state.Scope,
                Unit = null,
                LabelKey = "realtime.loadScope"
            });
        }

        // Today's Energy
        if (state.TodayEnergyKWh.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "loadTodayEnergy",
                Value = state.TodayEnergyKWh.Value,
                Unit = "kWh",
                LabelKey = "realtime.loadTodayEnergy"
            });
        }

        // Total Energy
        if (state.TotalEnergyKWh.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "loadTotalEnergy",
                Value = state.TotalEnergyKWh.Value,
                Unit = "kWh",
                LabelKey = "realtime.loadTotalEnergy"
            });
        }
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
                // Record device activity - device is online since RPC succeeded
                await _activityService.RecordActivityAsync(
                    tbDeviceId,
                    TelemetrySource.BatterySoc,
                    cancellationToken: cancellationToken);

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
