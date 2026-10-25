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
    private readonly IComponentDetailsCacheService _cacheService;
    private readonly ILogger<GetDeviceDetailsQueryHandler> _logger;
    private readonly IDemoDataService _demoDataService;
    private readonly IDemoSimulationDataService _demoSimulationService;

    public GetDeviceDetailsQueryHandler(
        IApplicationDbContext context,
        IThingsBoardConnectorService tbService,
        IDeviceMappingService deviceMapping,
        IDeviceActivityService activityService,
        IComponentDetailsCacheService cacheService,
        ILogger<GetDeviceDetailsQueryHandler> logger,
        IDemoDataService demoDataService,
        IDemoSimulationDataService demoSimulationService)
    {
        _context = context;
        _tbService = tbService;
        _deviceMapping = deviceMapping;
        _activityService = activityService;
        _cacheService = cacheService;
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

        // Step 2: Check if this is a demo device - return simulated data (no caching needed)
        var isDemoDevice = await _demoDataService.IsDemoDeviceAsync(component.DeviceId, cancellationToken);
        if (isDemoDevice)
        {
            return BuildDemoDeviceDetails(component);
        }

        // Step 3: Check cache for real devices (5-second TTL)
        // This allows multiple users to get the same data without duplicate RPC calls
        if (_cacheService.TryGetCachedDetails(request.ComponentId, out var cachedDetails))
        {
            _logger.LogDebug(
                "Returning cached component details for {ComponentId}",
                request.ComponentId);
            return cachedDetails;
        }

        // Step 4: Get ThingsBoard Device ID for real devices
        var tbDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(component.DeviceId, cancellationToken);
        if (tbDeviceId == null)
        {
            _logger.LogWarning("ThingsBoard device mapping not found for device {DeviceId}", component.DeviceId);
            return null;
        }

        // Step 5: Call appropriate RPC based on device type
        var response = new DeviceDetailsResponseDto();

        switch (component.DeviceType)
        {
            case DeviceType.Invertor:
                response.Details = await BuildInverterDetailsAsync(component, tbDeviceId.Value, cancellationToken);
                break;

            case DeviceType.BMS:
            case DeviceType.Battery:
                response.Details = await BuildBatteryDetailsAsync(component, tbDeviceId.Value, request.BmsAddress, cancellationToken);
                break;

            default:
                _logger.LogWarning("Unsupported device type {DeviceType} for component {ComponentId}",
                    component.DeviceType, component.Id);
                return null;
        }

        // Step 6: Cache the response (include bmsAddress in cache key if specified)
        var cacheKey = request.BmsAddress.HasValue
            ? $"{request.ComponentId}_{request.BmsAddress}"
            : request.ComponentId.ToString();
        _cacheService.CacheDetails(request.ComponentId, response);

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
        int? bmsAddress,
        CancellationToken cancellationToken)
    {
        var details = new List<DeviceDetailItemDto>();

        // Get battery energy flow state from RPC (includes BMS list)
        var energyFlowState = await GetBatteryEnergyFlowStateFromRpcAsync(tbDeviceId, cancellationToken);

        // Get battery info state from RPC for static data (serial numbers, firmware)
        var batteryInfo = await GetBatteryInfoFromRpcAsync(tbDeviceId, cancellationToken);

        if (bmsAddress.HasValue)
        {
            // Return specific BMS data
            BuildSpecificBmsDetails(details, energyFlowState, batteryInfo, bmsAddress.Value);
        }
        else
        {
            // Return aggregate data (legacy behavior)
            if (energyFlowState != null)
            {
                AddBatteryAggregateRealtimeDetails(details, energyFlowState);
                AddBmsListDetails(details, energyFlowState);
            }
            AddBatteryParameterDetails(details, component);
        }

        return details;
    }

    /// <summary>
    /// Builds detailed view for a specific BMS module
    /// </summary>
    private void BuildSpecificBmsDetails(
        List<DeviceDetailItemDto> details,
        BatteryEnergyFlowStateDto? energyFlowState,
        BatteryInfoStateDto? batteryInfo,
        int bmsAddress)
    {
        // Get live data for this specific BMS
        var bmsLiveData = energyFlowState?.BmsList?.FirstOrDefault(b => b.BmsAddress == bmsAddress);

        // Get static info for this specific BMS
        var bmsStaticInfo = batteryInfo?.BmsList?.FirstOrDefault(b => b.BmsAddress == bmsAddress);

        // ========== Static/Parameter Info ==========
        // Name based on BMS address
        details.Add(new DeviceDetailItemDto
        {
            Key = "name",
            Value = $"Battery Pack {bmsAddress}",
            Unit = null,
            LabelKey = "parameter.name"
        });

        // Manufacturer
        if (!string.IsNullOrEmpty(batteryInfo?.ManufacturerName))
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "manufacturer",
                Value = batteryInfo.ManufacturerName,
                Unit = null,
                LabelKey = "parameter.manufacturer"
            });
        }

        // Model (rated capacity)
        if (bmsStaticInfo?.RatedCapacityAh.HasValue == true)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "model",
                Value = $"{bmsStaticInfo.RatedCapacityAh}Ah",
                Unit = null,
                LabelKey = "parameter.model"
            });
        }

        // Serial Number
        if (!string.IsNullOrEmpty(bmsStaticInfo?.SerialNumber))
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "serialNumber",
                Value = bmsStaticInfo.SerialNumber,
                Unit = null,
                LabelKey = "parameter.serialNumber"
            });
        }

        // Firmware Version
        if (bmsStaticInfo?.SoftwareVersionMain.HasValue == true)
        {
            var firmwareVersion = bmsStaticInfo.SoftwareVersionSub.HasValue
                ? $"{bmsStaticInfo.SoftwareVersionMain}.{bmsStaticInfo.SoftwareVersionSub}"
                : bmsStaticInfo.SoftwareVersionMain.ToString();

            details.Add(new DeviceDetailItemDto
            {
                Key = "firmwareVersion",
                Value = firmwareVersion,
                Unit = null,
                LabelKey = "parameter.firmwareVersion"
            });
        }

        // Rated Capacity
        var ratedCapacity = bmsStaticInfo?.RatedCapacityAh ?? bmsLiveData?.RatedCapacityAh;
        if (ratedCapacity.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "ratedCapacity",
                Value = ratedCapacity.Value,
                Unit = "Ah",
                LabelKey = "parameter.ratedCapacity"
            });
        }

        // ========== Real-time Data ==========
        if (bmsLiveData == null)
        {
            return;
        }

        // State of Charge
        var soc = bmsLiveData.SocPercent;
        if (!soc.HasValue && bmsLiveData.BatteryRemainingCapacityAh.HasValue && ratedCapacity.HasValue && ratedCapacity > 0)
        {
            soc = bmsLiveData.BatteryRemainingCapacityAh.Value / ratedCapacity.Value * 100;
        }
        if (soc.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "soc",
                Value = Math.Round(soc.Value, 1),
                Unit = "%",
                LabelKey = "realtime.soc"
            });
        }

        // State of Health
        if (bmsLiveData.SohPercent.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "soh",
                Value = Math.Round(bmsLiveData.SohPercent.Value, 1),
                Unit = "%",
                LabelKey = "realtime.soh"
            });
        }

        // Charge/Discharge Status
        if (bmsLiveData.ChargeDischargeStatus.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "chargeDischargeStatus",
                Value = BmsDetailItemDto.GetStatusText(bmsLiveData.ChargeDischargeStatus),
                Unit = null,
                LabelKey = "realtime.chargeDischargeStatus"
            });
        }

        // Voltage
        if (bmsLiveData.BatteryPackVoltageV.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "voltage",
                Value = Math.Round(bmsLiveData.BatteryPackVoltageV.Value, 2),
                Unit = "V",
                LabelKey = "realtime.voltage"
            });
        }

        // Current
        if (bmsLiveData.CurrentA.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "current",
                Value = Math.Round(bmsLiveData.CurrentA.Value, 2),
                Unit = "A",
                LabelKey = "realtime.current"
            });
        }

        // Remaining Capacity
        if (bmsLiveData.BatteryRemainingCapacityAh.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "remainingCapacity",
                Value = Math.Round(bmsLiveData.BatteryRemainingCapacityAh.Value, 2),
                Unit = "Ah",
                LabelKey = "realtime.remainingCapacity"
            });
        }

        // Average Cell Temperature
        if (bmsLiveData.AvgCellTempC.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "temperature",
                Value = Math.Round(bmsLiveData.AvgCellTempC.Value, 1),
                Unit = "°C",
                LabelKey = "realtime.temperature"
            });
        }

        // Cell Voltage Info - Min
        if (bmsLiveData.MinCellVoltageMv.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "minCellVoltage",
                Value = bmsLiveData.MinCellVoltageMv.Value,
                Unit = "mV",
                LabelKey = "realtime.minCellVoltage"
            });
        }

        // Cell Voltage Info - Max
        if (bmsLiveData.MaxCellVoltageMv.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "maxCellVoltage",
                Value = bmsLiveData.MaxCellVoltageMv.Value,
                Unit = "mV",
                LabelKey = "realtime.maxCellVoltage"
            });
        }

        // Cell Voltage Info - Average
        if (bmsLiveData.AvgCellVoltageMv.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "avgCellVoltage",
                Value = bmsLiveData.AvgCellVoltageMv.Value,
                Unit = "mV",
                LabelKey = "realtime.avgCellVoltage"
            });
        }

        // Total Discharge Energy
        if (bmsLiveData.TotalDischargeWh.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "totalDischarge",
                Value = Math.Round(bmsLiveData.TotalDischargeWh.Value / 1000.0, 2),
                Unit = "kWh",
                LabelKey = "realtime.totalDischarge"
            });
        }

        // Cycle Count
        if (bmsLiveData.CycleCount.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "cycleCount",
                Value = bmsLiveData.CycleCount.Value,
                Unit = null,
                LabelKey = "realtime.cycleCount"
            });
        }
    }

    private async Task<BatteryInfoStateDto?> GetBatteryInfoFromRpcAsync(
        Guid tbDeviceId,
        CancellationToken cancellationToken)
    {
        try
        {
            var rpcResponse = await _tbService.SendTwoWayRpcAsync(
                tbDeviceId,
                "getBatteryInfoState",
                new Dictionary<string, object>(),
                timeoutMs: 10000,
                cancellationToken: cancellationToken);

            if (rpcResponse?.Success == true && rpcResponse.Response != null)
            {
                if (rpcResponse.Response.TryGetValue("data", out var dataObj) && dataObj != null)
                {
                    var dataJson = JsonSerializer.Serialize(dataObj);
                    return JsonSerializer.Deserialize<BatteryInfoStateDto>(dataJson);
                }

                var json = JsonSerializer.Serialize(rpcResponse.Response);
                return JsonSerializer.Deserialize<BatteryInfoStateDto>(json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get battery info from RPC for device {DeviceId}", tbDeviceId);
        }

        return null;
    }

    private async Task<BatteryEnergyFlowStateDto?> GetBatteryEnergyFlowStateFromRpcAsync(
        Guid tbDeviceId,
        CancellationToken cancellationToken)
    {
        try
        {
            var rpcResponse = await _tbService.SendTwoWayRpcAsync(
                tbDeviceId,
                "getBatteryEnergyFlowState",
                new Dictionary<string, object>(),
                timeoutMs: 10000,
                cancellationToken: cancellationToken);

            if (rpcResponse?.Success == true && rpcResponse.Response != null)
            {
                // Record device activity - device is online since RPC succeeded
                await _activityService.RecordActivityAsync(
                    tbDeviceId,
                    TelemetrySource.BatteryEnergyFlow,
                    cancellationToken: cancellationToken);

                // Response is wrapped: {"success": true, "data": {...}}
                // Extract the "data" property if present
                if (rpcResponse.Response.TryGetValue("data", out var dataObj) && dataObj != null)
                {
                    var dataJson = JsonSerializer.Serialize(dataObj);
                    return JsonSerializer.Deserialize<BatteryEnergyFlowStateDto>(dataJson);
                }

                // Fallback: try deserializing the whole response
                var json = JsonSerializer.Serialize(rpcResponse.Response);
                return JsonSerializer.Deserialize<BatteryEnergyFlowStateDto>(json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get battery energy flow state from RPC for device {DeviceId}", tbDeviceId);
        }

        return null;
    }

    private void AddBatteryAggregateRealtimeDetails(List<DeviceDetailItemDto> details, BatteryEnergyFlowStateDto state)
    {
        // State of Charge
        if (state.SocPercent.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "soc",
                Value = Math.Round(state.SocPercent.Value, 1),
                Unit = "%",
                LabelKey = "realtime.soc"
            });
        }

        // State of Health
        if (state.SohPercent.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "soh",
                Value = Math.Round(state.SohPercent.Value, 1),
                Unit = "%",
                LabelKey = "realtime.soh"
            });
        }

        // Voltage
        if (state.VoltageV.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "voltage",
                Value = Math.Round(state.VoltageV.Value, 2),
                Unit = "V",
                LabelKey = "realtime.voltage"
            });
        }

        // Current
        if (state.CurrentA.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "current",
                Value = Math.Round(state.CurrentA.Value, 2),
                Unit = "A",
                LabelKey = "realtime.current"
            });
        }

        // Temperature
        if (state.TemperatureC.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "temperature",
                Value = Math.Round(state.TemperatureC.Value, 1),
                Unit = "°C",
                LabelKey = "realtime.temperature"
            });
        }

        // BMS Count
        if (state.BmsCount.HasValue)
        {
            details.Add(new DeviceDetailItemDto
            {
                Key = "bmsCount",
                Value = state.BmsCount.Value,
                Unit = null,
                LabelKey = "realtime.bmsCount"
            });
        }
    }

    private void AddBmsListDetails(List<DeviceDetailItemDto> details, BatteryEnergyFlowStateDto state)
    {
        if (state.BmsList == null || state.BmsList.Count == 0)
        {
            return;
        }

        // Convert BMS list to detailed items with human-readable status text
        var bmsList = state.BmsList.Select(bms => new BmsDetailItemDto
        {
            BmsAddress = bms.BmsAddress,
            ChargeDischargeStatus = bms.ChargeDischargeStatus,
            ChargeDischargeStatusText = BmsDetailItemDto.GetStatusText(bms.ChargeDischargeStatus),
            CurrentA = bms.CurrentA.HasValue ? Math.Round(bms.CurrentA.Value, 2) : null,
            VoltageV = bms.BatteryPackVoltageV.HasValue ? Math.Round(bms.BatteryPackVoltageV.Value, 2) : null,
            RemainingCapacityAh = bms.BatteryRemainingCapacityAh.HasValue ? Math.Round(bms.BatteryRemainingCapacityAh.Value, 2) : null,
            MaxCellVoltageMv = bms.MaxCellVoltageMv,
            MinCellVoltageMv = bms.MinCellVoltageMv,
            AvgCellVoltageMv = bms.AvgCellVoltageMv,
            AvgCellTempC = bms.AvgCellTempC.HasValue ? Math.Round(bms.AvgCellTempC.Value, 1) : null,
            RatedCapacityAh = bms.RatedCapacityAh,
            TotalDischargeWh = bms.TotalDischargeWh,
            CycleCount = bms.CycleCount,
            SohPercent = bms.SohPercent.HasValue ? Math.Round(bms.SohPercent.Value, 1) : null,
            SocPercent = bms.SocPercent.HasValue ? Math.Round(bms.SocPercent.Value, 1) : null
        }).ToList();

        details.Add(new DeviceDetailItemDto
        {
            Key = "bmsList",
            Value = bmsList,
            Unit = null,
            LabelKey = "realtime.bmsList"
        });
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

    #endregion
}
