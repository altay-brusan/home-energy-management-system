using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Hems_Backend.Application.DeviceComponents.Queries.GetDeviceSummary;

public class GetDeviceSummaryQueryHandler : IRequestHandler<GetDeviceSummaryQuery, DeviceSummaryResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IThingsBoardConnectorService _tbService;
    private readonly IDeviceMappingService _deviceMapping;
    private readonly IDeviceActivityService _activityService;
    private readonly ILogger<GetDeviceSummaryQueryHandler> _logger;

    public GetDeviceSummaryQueryHandler(
        IApplicationDbContext context,
        IThingsBoardConnectorService tbService,
        IDeviceMappingService deviceMapping,
        IDeviceActivityService activityService,
        ILogger<GetDeviceSummaryQueryHandler> logger)
    {
        _context = context;
        _tbService = tbService;
        _deviceMapping = deviceMapping;
        _activityService = activityService;
        _logger = logger;
    }

    public async Task<DeviceSummaryResponseDto> Handle(GetDeviceSummaryQuery request, CancellationToken cancellationToken)
    {
        var response = new DeviceSummaryResponseDto();

        // Get ThingsBoard device ID
        var tbDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(request.DeviceId, cancellationToken);
        if (tbDeviceId == null)
        {
            _logger.LogWarning("ThingsBoard device mapping not found for device {DeviceId}", request.DeviceId);
            return response;
        }

        // Get all components for this device
        var components = await _context.DeviceComponents
            .Where(c => c.DeviceId == request.DeviceId && !c.IsDeleted)
            .ToListAsync(cancellationToken);

        // Process Inverter
        var inverterComponent = components.FirstOrDefault(c => c.DeviceType == DeviceType.Invertor);
        var inverterSummary = await BuildInverterSummaryAsync(inverterComponent, tbDeviceId.Value, cancellationToken);
        if (inverterSummary != null)
        {
            response.Components.Add(inverterSummary);
        }

        // Process Batteries
        var batteryComponents = components.Where(c => c.DeviceType == DeviceType.BMS).ToList();
        var batterySummary = await BuildBatterySummaryAsync(batteryComponents, tbDeviceId.Value, cancellationToken);
        if (batterySummary != null)
        {
            response.Components.Add(batterySummary);
        }

        return response;
    }

    #region Inverter Logic

    private async Task<InverterSummaryDto?> BuildInverterSummaryAsync(
        Domain.Entities.DeviceComponent? component,
        Guid tbDeviceId,
        CancellationToken cancellationToken)
    {
        // Step 1: Try to get inverter info from device via RPC
        var inverterInfo = await GetInverterInfoFromRpcAsync(tbDeviceId, cancellationToken);

        // Step 2: If RPC succeeded, update DB with fresh data
        if (inverterInfo != null)
        {
            component = await UpdateOrCreateInverterComponentAsync(
                component,
                inverterInfo,
                tbDeviceId,
                cancellationToken);
        }

        // Step 3: Build response using component data (either updated or fallback from DB)
        if (component == null)
        {
            _logger.LogWarning("No inverter component found and RPC failed for device {DeviceId}", tbDeviceId);
            return null;
        }

        var inverter = new InverterSummaryDto
        {
            Id = component.Id,
            Type = "inverter",
            Name = component.Name,
            Model = component.ModelNumber ?? "Generic",
            Manufacturer = component.Manufacturer ?? "NetZr",
            SerialNumber = component.SerialNumber ?? "Generic"
        };

        // Get realtime active power from RPC
        var state = await GetInverterDeviceStateFromRpcAsync(tbDeviceId, cancellationToken);
        if (state?.GridSideActivePowerW != null)
        {
            inverter.ActivePowerKw = Math.Round(state.GridSideActivePowerW.Value / 1000.0, 2);
        }

        return inverter;
    }

    /// <summary>
    /// Updates existing inverter component or creates new one with data from RPC
    /// </summary>
    private async Task<Domain.Entities.DeviceComponent?> UpdateOrCreateInverterComponentAsync(
        Domain.Entities.DeviceComponent? component,
        InverterInfoStateDto inverterInfo,
        Guid tbDeviceId,
        CancellationToken cancellationToken)
    {
        // Build specifications JSON from inverter info
        var specifications = BuildInverterSpecifications(inverterInfo);

        if (component != null)
        {
            // Update existing component
            var needsUpdate = false;

            // Update serial number if available from RPC
            if (!string.IsNullOrEmpty(inverterInfo.SerialNumber) && component.SerialNumber != inverterInfo.SerialNumber)
            {
                component.SerialNumber = inverterInfo.SerialNumber;
                needsUpdate = true;
            }

            // Update model number based on device type
            var modelNumber = GetInverterModelNumber(inverterInfo);
            if (!string.IsNullOrEmpty(modelNumber) && component.ModelNumber != modelNumber)
            {
                component.ModelNumber = modelNumber;
                needsUpdate = true;
            }

            // Always update specifications with latest RPC data
            component.Specifications = JsonDocument.Parse(specifications);
            needsUpdate = true;

            if (needsUpdate)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation(
                    "Updated inverter component {ComponentId} with RPC data: SerialNumber={SerialNumber}, Model={Model}",
                    component.Id, component.SerialNumber, component.ModelNumber);
            }

            return component;
        }
        else
        {
            // Create new component
            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.ThingsBoardDeviceId == tbDeviceId, cancellationToken);

            if (device == null)
            {
                _logger.LogWarning("Device not found for ThingsBoard ID {TbDeviceId}", tbDeviceId);
                return null;
            }

            component = new Domain.Entities.DeviceComponent
            {
                Id = Guid.NewGuid(),
                DeviceId = device.Id,
                DeviceType = DeviceType.Invertor,
                Name = inverterInfo.DeviceType ?? "Hybrid Inverter",
                Manufacturer = "NetZr", // Default manufacturer
                ModelNumber = GetInverterModelNumber(inverterInfo) ?? "Generic",
                SerialNumber = inverterInfo.SerialNumber ?? "Generic",
                Specifications = JsonDocument.Parse(specifications),
                IsDeleted = false
            };

            _context.DeviceComponents.Add(component);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Created inverter component {ComponentId} for device {DeviceId} from RPC data",
                component.Id, device.Id);

            return component;
        }
    }

    /// <summary>
    /// Builds specifications JSON from inverter info RPC response
    /// </summary>
    private string BuildInverterSpecifications(InverterInfoStateDto info)
    {
        var specs = new Dictionary<string, object?>();

        if (!string.IsNullOrEmpty(info.DeviceType)) specs["deviceType"] = info.DeviceType;
        if (info.DeviceTypeCode.HasValue) specs["deviceTypeCode"] = info.DeviceTypeCode;
        if (info.RatedPowerW.HasValue) specs["ratedPowerW"] = info.RatedPowerW;
        if (info.RatedPowerKw.HasValue) specs["ratedPowerKw"] = info.RatedPowerKw;
        if (info.MpptCount.HasValue) specs["mpptCount"] = info.MpptCount;
        if (info.PhaseCount.HasValue) specs["phaseCount"] = info.PhaseCount;
        if (info.ModbusAddress.HasValue) specs["modbusAddress"] = info.ModbusAddress;
        if (!string.IsNullOrEmpty(info.ProtocolVersion)) specs["protocolVersion"] = info.ProtocolVersion;
        if (!string.IsNullOrEmpty(info.GridVoltageRating)) specs["gridVoltageRating"] = info.GridVoltageRating;
        if (!string.IsNullOrEmpty(info.SafetyType)) specs["safetyType"] = info.SafetyType;
        if (info.Firmware != null && info.Firmware.Any()) specs["firmware"] = info.Firmware;
        if (!string.IsNullOrEmpty(info.ControlBoard)) specs["controlBoard"] = info.ControlBoard;
        if (info.ControlHardware.HasValue) specs["controlHardware"] = info.ControlHardware;
        if (info.ControlMasterVersion.HasValue) specs["controlMasterVersion"] = info.ControlMasterVersion;
        if (!string.IsNullOrEmpty(info.CommunicationBoard)) specs["communicationBoard"] = info.CommunicationBoard;
        if (info.CommMasterVersion.HasValue) specs["commMasterVersion"] = info.CommMasterVersion;
        if (!string.IsNullOrEmpty(info.ChipType)) specs["chipType"] = info.ChipType;
        if (!string.IsNullOrEmpty(info.RatedPowerClass)) specs["ratedPowerClass"] = info.RatedPowerClass;

        return JsonSerializer.Serialize(specs);
    }

    /// <summary>
    /// Derives model number from inverter info
    /// </summary>
    private string? GetInverterModelNumber(InverterInfoStateDto info)
    {
        // Use device type as model number if available
        if (!string.IsNullOrEmpty(info.DeviceType))
        {
            return info.DeviceType;
        }

        // Fallback: construct from rated power
        if (info.RatedPowerKw.HasValue)
        {
            return $"{info.RatedPowerKw}kW Hybrid";
        }

        return null;
    }

    private async Task<InverterInfoStateDto?> GetInverterInfoFromRpcAsync(
        Guid tbDeviceId,
        CancellationToken cancellationToken)
    {
        try
        {
            var rpcResponse = await _tbService.SendTwoWayRpcAsync(
                tbDeviceId,
                "getInverterInfo",
                new Dictionary<string, object>(),
                timeoutMs: 10000,
                cancellationToken: cancellationToken);

            if (rpcResponse?.Success == true && rpcResponse.Response != null)
            {
                // Response may be wrapped: {"success": true, "data": {...}}
                if (rpcResponse.Response.TryGetValue("data", out var dataObj) && dataObj != null)
                {
                    var dataJson = JsonSerializer.Serialize(dataObj);
                    return JsonSerializer.Deserialize<InverterInfoStateDto>(dataJson);
                }

                // Fallback: try deserializing the whole response
                var json = JsonSerializer.Serialize(rpcResponse.Response);
                return JsonSerializer.Deserialize<InverterInfoStateDto>(json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get inverter info from RPC for device {DeviceId}", tbDeviceId);
        }

        return null;
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
            _logger.LogWarning(ex, "Failed to get inverter state from RPC for device {DeviceId}", tbDeviceId);
        }

        return null;
    }

    #endregion

    #region Battery Logic

    private async Task<BatterySummaryDto?> BuildBatterySummaryAsync(
        List<Domain.Entities.DeviceComponent> components,
        Guid tbDeviceId,
        CancellationToken cancellationToken)
    {
        // Step 1: Try to get battery info from device via RPC (static info: manufacturer, serial numbers)
        var batteryInfo = await GetBatteryInfoFromRpcAsync(tbDeviceId, cancellationToken);

        // Step 2: Get fresh energy flow state from device (live data with per-BMS details)
        var energyFlowState = await GetBatteryEnergyFlowStateFromRpcAsync(tbDeviceId, cancellationToken);

        // Step 3: If we got battery info from RPC, update DB
        if (batteryInfo != null)
        {
            components = await UpdateOrCreateBatteryComponentsAsync(
                components,
                batteryInfo,
                tbDeviceId,
                cancellationToken);
        }

        // Step 4: Build response using component data
        if (!components.Any())
        {
            // No battery components found and RPC failed - check if we have energy flow data
            if (energyFlowState == null)
            {
                return null;
            }

            // Create generic components from energy flow data as fallback
            components = await CreateBatteryComponentsFromEnergyFlowStateAsync(
                energyFlowState,
                tbDeviceId,
                cancellationToken);

            if (!components.Any())
            {
                return null;
            }
        }

        return BuildBatterySummaryFromComponents(components, energyFlowState, batteryInfo);
    }

    /// <summary>
    /// Updates existing battery components or creates new ones with data from RPC
    /// </summary>
    private async Task<List<Domain.Entities.DeviceComponent>> UpdateOrCreateBatteryComponentsAsync(
        List<Domain.Entities.DeviceComponent> existingComponents,
        BatteryInfoStateDto batteryInfo,
        Guid tbDeviceId,
        CancellationToken cancellationToken)
    {
        // Get the device from ThingsBoard mapping
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.ThingsBoardDeviceId == tbDeviceId, cancellationToken);

        if (device == null)
        {
            _logger.LogWarning("Device not found for ThingsBoard ID {TbDeviceId}", tbDeviceId);
            return existingComponents;
        }

        // Build specifications JSON from battery info
        var specifications = BuildBatterySpecifications(batteryInfo);

        // Get master BMS (first one, bmsAddress = 1) serial number
        var masterBms = batteryInfo.BmsList?.FirstOrDefault(b => b.BmsAddress == 1)
                        ?? batteryInfo.BmsList?.FirstOrDefault();
        var masterSerialNumber = masterBms?.SerialNumber;

        if (existingComponents.Any())
        {
            // Update existing component (use first one as the main battery component)
            var mainComponent = existingComponents.First();
            var needsUpdate = false;

            // Always update manufacturer if available from RPC
            if (!string.IsNullOrEmpty(batteryInfo.ManufacturerName) && mainComponent.Manufacturer != batteryInfo.ManufacturerName)
            {
                mainComponent.Manufacturer = batteryInfo.ManufacturerName;
                needsUpdate = true;
            }

            // Update serial number with master BMS serial number
            if (!string.IsNullOrEmpty(masterSerialNumber) && mainComponent.SerialNumber != masterSerialNumber)
            {
                mainComponent.SerialNumber = masterSerialNumber;
                needsUpdate = true;
            }

            // Update model number with calculated value
            var modelNumber = GetBatteryModelNumber(batteryInfo);
            if (!string.IsNullOrEmpty(modelNumber) && mainComponent.ModelNumber != modelNumber)
            {
                mainComponent.ModelNumber = modelNumber;
                needsUpdate = true;
            }

            // Always update specifications with latest RPC data
            mainComponent.Specifications = JsonDocument.Parse(specifications);
            needsUpdate = true;

            if (needsUpdate)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation(
                    "Updated battery component {ComponentId} with RPC data: Manufacturer={Manufacturer}, SerialNumber={SerialNumber}, Model={Model}",
                    mainComponent.Id, mainComponent.Manufacturer, mainComponent.SerialNumber, mainComponent.ModelNumber);
            }

            return existingComponents;
        }
        else
        {
            // Create new battery component
            var component = new Domain.Entities.DeviceComponent
            {
                Id = Guid.NewGuid(),
                DeviceId = device.Id,
                DeviceType = DeviceType.BMS,
                Name = "Battery Management System",
                Manufacturer = batteryInfo.ManufacturerName ?? "NetZr",
                ModelNumber = GetBatteryModelNumber(batteryInfo),
                SerialNumber = masterSerialNumber ?? "Generic",
                Specifications = JsonDocument.Parse(specifications),
                IsDeleted = false
            };

            _context.DeviceComponents.Add(component);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Created battery component {ComponentId} for device {DeviceId} from RPC data: Manufacturer={Manufacturer}, SerialNumber={SerialNumber}",
                component.Id, device.Id, component.Manufacturer, component.SerialNumber);

            return new List<Domain.Entities.DeviceComponent> { component };
        }
    }

    /// <summary>
    /// Creates battery components from energy flow state when getBatteryInfoState fails
    /// </summary>
    private async Task<List<Domain.Entities.DeviceComponent>> CreateBatteryComponentsFromEnergyFlowStateAsync(
        BatteryEnergyFlowStateDto energyFlowState,
        Guid tbDeviceId,
        CancellationToken cancellationToken)
    {
        var components = new List<Domain.Entities.DeviceComponent>();

        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.ThingsBoardDeviceId == tbDeviceId, cancellationToken);

        if (device == null)
        {
            return components;
        }

        var hasBmsData = energyFlowState.BmsList != null && energyFlowState.BmsList.Any();
        var hasAggregateSoc = energyFlowState.SocPercent.HasValue;

        if (!hasBmsData && !hasAggregateSoc)
        {
            return components;
        }

        var component = new Domain.Entities.DeviceComponent
        {
            Id = Guid.NewGuid(),
            DeviceId = device.Id,
            DeviceType = DeviceType.BMS,
            Name = "Battery Management System",
            Manufacturer = "NetZr",
            ModelNumber = hasBmsData
                ? $"{energyFlowState.BmsCount ?? energyFlowState.BmsList!.Count} Battery Modules"
                : "Generic",
            SerialNumber = "Generic",
            Specifications = JsonDocument.Parse("{}"),
            IsDeleted = false
        };

        _context.DeviceComponents.Add(component);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created fallback battery component {ComponentId} for device {DeviceId} from energy flow state",
            component.Id, device.Id);

        components.Add(component);
        return components;
    }

    /// <summary>
    /// Builds battery summary response from components and optional RPC data
    /// </summary>
    private BatterySummaryDto BuildBatterySummaryFromComponents(
        List<Domain.Entities.DeviceComponent> components,
        BatteryEnergyFlowStateDto? energyFlowState,
        BatteryInfoStateDto? batteryInfo)
    {
        var battery = new BatterySummaryDto
        {
            Type = "battery"
        };

        var mainComponent = components.First();
        battery.Id = mainComponent.Id;
        battery.Name = mainComponent.Name;
        battery.Model = mainComponent.ModelNumber ?? "Generic";
        battery.Manufacturer = mainComponent.Manufacturer ?? "NetZr";
        battery.SerialNumber = mainComponent.SerialNumber ?? "Generic";

        // Build packs list - prefer batteryInfo for static data, energyFlowState for live data
        if (batteryInfo?.BmsList != null && batteryInfo.BmsList.Any())
        {
            _logger.LogDebug("Building battery packs from batteryInfo.BmsList with {Count} items", batteryInfo.BmsList.Count);

            // Use getBatteryInfoState data for packs + merge SOC/SOH from energyFlowState
            foreach (var bmsInfo in batteryInfo.BmsList)
            {
                // Find matching live data by BMS address from getBatteryEnergyFlowState
                var bmsLiveData = energyFlowState?.BmsList?.FirstOrDefault(b => b.BmsAddress == bmsInfo.BmsAddress);

                // Build firmware version string
                string? firmwareVersion = null;
                if (bmsInfo.SoftwareVersionMain.HasValue)
                {
                    firmwareVersion = bmsInfo.SoftwareVersionSub.HasValue
                        ? $"{bmsInfo.SoftwareVersionMain}.{bmsInfo.SoftwareVersionSub}"
                        : bmsInfo.SoftwareVersionMain.ToString();
                }

                battery.Packs.Add(new BatteryPackDto
                {
                    Id = bmsInfo.SerialNumber ?? $"bms-{bmsInfo.BmsAddress}",
                    Type = "battery",
                    Name = $"Battery Pack {bmsInfo.BmsAddress}",
                    Model = bmsInfo.RatedCapacityAh.HasValue ? $"{bmsInfo.RatedCapacityAh}Ah" : "Generic",
                    Manufacturer = batteryInfo.ManufacturerName ?? "NetZr",
                    SerialNumber = bmsInfo.SerialNumber ?? "Generic",
                    ComponentId = mainComponent.Id,
                    BmsAddress = bmsInfo.BmsAddress,
                    StateOfCharge = bmsLiveData?.SocPercent.HasValue == true
                        ? Math.Round(bmsLiveData.SocPercent!.Value, 1)
                        : (bmsLiveData?.BatteryRemainingCapacityAh.HasValue == true && bmsInfo.RatedCapacityAh.HasValue && bmsInfo.RatedCapacityAh > 0
                            ? Math.Round(bmsLiveData.BatteryRemainingCapacityAh!.Value / bmsInfo.RatedCapacityAh.Value * 100, 1)
                            : null),
                    StateOfHealth = bmsLiveData?.SohPercent.HasValue == true
                        ? Math.Round(bmsLiveData.SohPercent!.Value, 1)
                        : null,
                    Voltage = bmsLiveData?.BatteryPackVoltageV.HasValue == true
                        ? Math.Round(bmsLiveData.BatteryPackVoltageV!.Value, 2)
                        : null,
                    Current = bmsLiveData?.CurrentA.HasValue == true
                        ? Math.Round(bmsLiveData.CurrentA!.Value, 2)
                        : null,
                    Temperature = bmsLiveData?.AvgCellTempC.HasValue == true
                        ? Math.Round(bmsLiveData.AvgCellTempC!.Value, 1)
                        : null,
                    FirmwareVersion = firmwareVersion
                });
            }
        }
        else if (energyFlowState?.BmsList != null && energyFlowState.BmsList.Any())
        {
            _logger.LogDebug("Building battery packs from energyFlowState.BmsList with {Count} items (fallback)", energyFlowState.BmsList.Count);

            // Fallback: Use energy flow state data for packs (when getBatteryInfoState fails)
            foreach (var bmsLiveData in energyFlowState.BmsList)
            {
                battery.Packs.Add(new BatteryPackDto
                {
                    Id = $"bms-{bmsLiveData.BmsAddress}",
                    Type = "battery",
                    Name = $"Battery Pack {bmsLiveData.BmsAddress}",
                    Model = bmsLiveData.RatedCapacityAh.HasValue ? $"{bmsLiveData.RatedCapacityAh}Ah" : "Generic",
                    Manufacturer = mainComponent.Manufacturer ?? "NetZr",
                    SerialNumber = "Generic",
                    ComponentId = mainComponent.Id,
                    BmsAddress = bmsLiveData.BmsAddress,
                    StateOfCharge = bmsLiveData.SocPercent.HasValue
                        ? Math.Round(bmsLiveData.SocPercent.Value, 1)
                        : (bmsLiveData.BatteryRemainingCapacityAh.HasValue && bmsLiveData.RatedCapacityAh.HasValue && bmsLiveData.RatedCapacityAh > 0
                            ? Math.Round(bmsLiveData.BatteryRemainingCapacityAh.Value / bmsLiveData.RatedCapacityAh.Value * 100, 1)
                            : null),
                    StateOfHealth = bmsLiveData.SohPercent.HasValue
                        ? Math.Round(bmsLiveData.SohPercent.Value, 1)
                        : null,
                    Voltage = bmsLiveData.BatteryPackVoltageV.HasValue
                        ? Math.Round(bmsLiveData.BatteryPackVoltageV.Value, 2)
                        : null,
                    Current = bmsLiveData.CurrentA.HasValue
                        ? Math.Round(bmsLiveData.CurrentA.Value, 2)
                        : null,
                    Temperature = bmsLiveData.AvgCellTempC.HasValue
                        ? Math.Round(bmsLiveData.AvgCellTempC.Value, 1)
                        : null,
                    FirmwareVersion = null
                });
            }
        }
        else
        {
            _logger.LogDebug("Building single generic battery pack (no BmsList available)");

            // Single generic pack using aggregate data
            battery.Packs.Add(new BatteryPackDto
            {
                Id = mainComponent.Id.ToString(),
                Type = "battery",
                Name = mainComponent.Name,
                Model = mainComponent.ModelNumber ?? "Generic",
                Manufacturer = mainComponent.Manufacturer ?? "NetZr",
                SerialNumber = mainComponent.SerialNumber ?? "Generic",
                ComponentId = mainComponent.Id,
                BmsAddress = 1,
                StateOfCharge = energyFlowState?.SocPercent.HasValue == true
                    ? Math.Round(energyFlowState.SocPercent!.Value, 1)
                    : null,
                StateOfHealth = energyFlowState?.SohPercent.HasValue == true
                    ? Math.Round(energyFlowState.SohPercent!.Value, 1)
                    : null,
                Voltage = energyFlowState?.VoltageV.HasValue == true
                    ? Math.Round(energyFlowState.VoltageV!.Value, 2)
                    : null,
                Current = energyFlowState?.CurrentA.HasValue == true
                    ? Math.Round(energyFlowState.CurrentA!.Value, 2)
                    : null,
                Temperature = energyFlowState?.TemperatureC.HasValue == true
                    ? Math.Round(energyFlowState.TemperatureC!.Value, 1)
                    : null,
                FirmwareVersion = null
            });
        }

        // Calculate aggregate SOC (average of all packs with SOC data, or use aggregate from energyFlowState)
        var packsWithSoc = battery.Packs.Where(p => p.StateOfCharge.HasValue).ToList();
        if (packsWithSoc.Any())
        {
            battery.StateOfCharge = Math.Round(packsWithSoc.Average(p => p.StateOfCharge!.Value), 1);
        }
        else if (energyFlowState?.SocPercent.HasValue == true)
        {
            battery.StateOfCharge = Math.Round(energyFlowState.SocPercent.Value, 1);
        }

        return battery;
    }

    /// <summary>
    /// Builds specifications JSON from battery info RPC response
    /// </summary>
    private string BuildBatterySpecifications(BatteryInfoStateDto info)
    {
        var specs = new Dictionary<string, object?>();

        if (!string.IsNullOrEmpty(info.ManufacturerName)) specs["manufacturerName"] = info.ManufacturerName;
        if (info.ModuleCount.HasValue) specs["moduleCount"] = info.ModuleCount;

        // Add BMS list details
        if (info.BmsList != null && info.BmsList.Any())
        {
            var bmsList = info.BmsList.Select(bms => new Dictionary<string, object?>
            {
                { "bmsAddress", bms.BmsAddress },
                { "serialNumber", bms.SerialNumber },
                { "ratedCapacityAh", bms.RatedCapacityAh },
                { "softwareVersionMain", bms.SoftwareVersionMain },
                { "softwareVersionSub", bms.SoftwareVersionSub }
            }).ToList();

            specs["bmsList"] = bmsList;

            // Calculate total capacity
            var totalCapacityAh = info.BmsList
                .Where(b => b.RatedCapacityAh.HasValue)
                .Sum(b => b.RatedCapacityAh!.Value);
            if (totalCapacityAh > 0)
            {
                specs["totalCapacityAh"] = totalCapacityAh;
                // Assume 51.2V nominal voltage for LiFePO4
                specs["totalCapacityKWh"] = Math.Round(totalCapacityAh * 51.2 / 1000, 2);
            }
        }

        return JsonSerializer.Serialize(specs);
    }

    /// <summary>
    /// Derives model number from battery info
    /// </summary>
    private string GetBatteryModelNumber(BatteryInfoStateDto info)
    {
        // Use manufacturer + module count as model
        if (info.ModuleCount.HasValue && info.ModuleCount > 0)
        {
            var totalCapacityAh = info.BmsList?
                .Where(b => b.RatedCapacityAh.HasValue)
                .Sum(b => b.RatedCapacityAh!.Value) ?? 0;

            if (totalCapacityAh > 0)
            {
                var totalKwh = Math.Round(totalCapacityAh * 51.2 / 1000, 1);
                return $"{totalKwh}kWh ({info.ModuleCount} modules)";
            }

            return $"{info.ModuleCount} Battery Modules";
        }

        return "Generic";
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
                // Log raw response for debugging
                var rawJson = JsonSerializer.Serialize(rpcResponse.Response);
                _logger.LogDebug("getBatteryInfoState raw response: {RawJson}", rawJson);

                // Response may be wrapped: {"success": true, "data": {...}} or {"response": {...}}
                object? dataObj = null;
                if (rpcResponse.Response.TryGetValue("data", out dataObj) && dataObj != null)
                {
                    // Wrapped in "data"
                }
                else if (rpcResponse.Response.TryGetValue("response", out dataObj) && dataObj != null)
                {
                    // Wrapped in "response"
                }
                else
                {
                    // Not wrapped, use the whole response
                    dataObj = rpcResponse.Response;
                }

                var dataJson = JsonSerializer.Serialize(dataObj);
                var result = JsonSerializer.Deserialize<BatteryInfoStateDto>(dataJson);

                // Log parsed result for debugging
                _logger.LogDebug(
                    "getBatteryInfoState parsed: ManufacturerName={Manufacturer}, ModuleCount={ModuleCount}, BmsListCount={BmsListCount}",
                    result?.ManufacturerName,
                    result?.ModuleCount,
                    result?.BmsList?.Count ?? 0);

                return result;
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

                // Log raw response for debugging
                var rawJson = JsonSerializer.Serialize(rpcResponse.Response);
                _logger.LogDebug("getBatteryEnergyFlowState raw response: {RawJson}", rawJson);

                // Response may be wrapped: {"success": true, "data": {...}} or {"response": {...}}
                object? dataObj = null;
                if (rpcResponse.Response.TryGetValue("data", out dataObj) && dataObj != null)
                {
                    // Wrapped in "data"
                }
                else if (rpcResponse.Response.TryGetValue("response", out dataObj) && dataObj != null)
                {
                    // Wrapped in "response"
                }
                else
                {
                    // Not wrapped, use the whole response
                    dataObj = rpcResponse.Response;
                }

                var dataJson = JsonSerializer.Serialize(dataObj);
                var result = JsonSerializer.Deserialize<BatteryEnergyFlowStateDto>(dataJson);

                // Log parsed result for debugging
                _logger.LogDebug(
                    "getBatteryEnergyFlowState parsed: BmsCount={BmsCount}, SocPercent={Soc}, BmsListCount={BmsListCount}",
                    result?.BmsCount,
                    result?.SocPercent,
                    result?.BmsList?.Count ?? 0);

                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get battery energy flow state from RPC for device {DeviceId}", tbDeviceId);
        }

        return null;
    }

    #endregion
}
