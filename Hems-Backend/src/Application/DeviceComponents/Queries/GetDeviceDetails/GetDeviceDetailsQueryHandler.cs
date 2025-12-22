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

    public GetDeviceDetailsQueryHandler(
        IApplicationDbContext context,
        IThingsBoardConnectorService tbService,
        IDeviceMappingService deviceMapping,
        ILogger<GetDeviceDetailsQueryHandler> logger)
    {
        _context = context;
        _tbService = tbService;
        _deviceMapping = deviceMapping;
        _logger = logger;
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

        // Step 2: Get ThingsBoard Device ID
        var tbDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(component.DeviceId, cancellationToken);
        if (tbDeviceId == null)
        {
            _logger.LogWarning("ThingsBoard device mapping not found for device {DeviceId}", component.DeviceId);
            return null;
        }

        // Step 3: Call appropriate RPC based on device type
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
