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
    private readonly ILogger<GetDeviceSummaryQueryHandler> _logger;

    public GetDeviceSummaryQueryHandler(
        IApplicationDbContext context,
        IThingsBoardConnectorService tbService,
        IDeviceMappingService deviceMapping,
        ILogger<GetDeviceSummaryQueryHandler> logger)
    {
        _context = context;
        _tbService = tbService;
        _deviceMapping = deviceMapping;
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
        InverterIdentityDto? identity = null;

        // Try to get identity from component DB first, otherwise call RPC
        if (component == null || string.IsNullOrEmpty(component.Name) || string.IsNullOrEmpty(component.Manufacturer))
        {
            identity = await GetInverterInfoFromRpcAsync(tbDeviceId, cancellationToken);

            // If we got identity from RPC and no component exists in DB, create one
            if (identity != null && component == null)
            {
                // Get the device from ThingsBoard mapping to get the actual device ID
                var device = await _context.Devices
                    .FirstOrDefaultAsync(d => d.ThingsBoardDeviceId == tbDeviceId, cancellationToken);

                if (device != null)
                {
                    component = new Domain.Entities.DeviceComponent
                    {
                        Id = Guid.NewGuid(),
                        DeviceId = device.Id,
                        DeviceType = DeviceType.Invertor,
                        Name = identity.Name ?? "Generic Inverter",
                        Manufacturer = identity.Manufacturer ?? "Ottomotive",
                        ModelNumber = identity.ModelNumber ?? "Generic",
                        SerialNumber = identity.SerialNumber ?? "Generic",
                        Specifications = identity.AdditionalProperties != null && identity.AdditionalProperties.Any()
                            ? JsonDocument.Parse(JsonSerializer.Serialize(identity.AdditionalProperties))
                            : JsonDocument.Parse("{}"),
                        IsDeleted = false
                    };

                    _context.DeviceComponents.Add(component);
                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation(
                        "Created generic inverter component {ComponentId} for device {DeviceId} from RPC data",
                        component.Id, device.Id);
                }
            }
        }

        var inverter = new InverterSummaryDto
        {
            Id = component?.Id ?? Guid.NewGuid(),
            Type = "inverter",
            Name = component?.Name ?? identity?.Name ?? "Generic",
            Model = component?.ModelNumber ?? identity?.ModelNumber ?? "Generic",
            Manufacturer = component?.Manufacturer ?? identity?.Manufacturer ?? "Ottomotive",
            SerialNumber = component?.SerialNumber ?? identity?.SerialNumber ?? "Generic"
        };

        // Always get fresh state data (active power) from RPC
        var state = await GetInverterDeviceStateFromRpcAsync(tbDeviceId, cancellationToken);
        if (state?.GridSideActivePowerW != null)
        {
            inverter.ActivePowerKw = state.GridSideActivePowerW / 1000.0;
        }

        return inverter;
    }

    private async Task<InverterIdentityDto?> GetInverterInfoFromRpcAsync(
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
                cancellationToken);

            if (rpcResponse?.Success == true && rpcResponse.Response != null)
            {
                var json = JsonSerializer.Serialize(rpcResponse.Response);
                return JsonSerializer.Deserialize<InverterIdentityDto>(json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get inverter identity from RPC for device {DeviceId}", tbDeviceId);
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
                cancellationToken);

            if (rpcResponse?.Success == true && rpcResponse.Response != null)
            {
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
        // Step 1: Check if device has registered battery components in DB
        bool hasBatteriesInDb = components.Any();

        // Step 2: Always get fresh SoC state from device (live data)
        var socState = await GetBatterySocStateFromRpcAsync(tbDeviceId, cancellationToken);

        if (socState == null)
        {
            // No battery data available from device
            return null;
        }

        // Determine the type of battery data we have
        var hasDetailedBmsData = socState.BmsSocDetails != null && socState.BmsSocDetails.Any();
        var hasInverterLevelData = socState.InverterBatChannelSoc != null &&
                                   socState.InverterBatChannelSoc.Any(c => c.SocPercent.HasValue);

        if (!hasDetailedBmsData && !hasInverterLevelData)
        {
            // No battery data from device
            return null;
        }

        // Step 3: Handle registered batteries vs creating generic ones
        if (hasBatteriesInDb)
        {
            // Device has registered batteries - use them and match with SoC data
            return BuildRegisteredBatterySummaryAsync(components, socState, hasDetailedBmsData);
        }
        else
        {
            // No batteries registered - create generic battery component(s)
            return await CreateGenericBatterySummaryAsync(
                tbDeviceId,
                socState,
                hasDetailedBmsData,
                hasInverterLevelData,
                cancellationToken);
        }
    }

    /// <summary>
    /// Builds battery summary using registered components from DB + fresh SoC data
    /// </summary>
    private BatterySummaryDto BuildRegisteredBatterySummaryAsync(
        List<Domain.Entities.DeviceComponent> components,
        BatterySocStateDto socState,
        bool hasDetailedBmsData)
    {
        var battery = new BatterySummaryDto
        {
            Type = "battery"
        };

        if (hasDetailedBmsData && socState.BmsSocDetails != null)
        {
            // We have detailed BMS data - match with registered components
            var allSocs = socState.BmsSocDetails
                .Where(d => d.SocPercent.HasValue)
                .Select(d => d.SocPercent!.Value)
                .ToList();

            // Use first registered component for main battery info
            var mainComponent = components.First();
            battery.Id = mainComponent.Id;
            battery.Name = mainComponent.Name;
            battery.Model = mainComponent.ModelNumber ?? "Generic";
            battery.Manufacturer = mainComponent.Manufacturer ?? "Ottomotive";
            battery.SerialNumber = mainComponent.SerialNumber ?? "Generic";
            battery.StateOfCharge = allSocs.Any() ? allSocs.Average() : null;

            // Match BMS details with registered components
            foreach (var bmsDetail in socState.BmsSocDetails)
            {
                // Try to find matching component by serial number or device ID
                var matchingComponent = components.FirstOrDefault(c =>
                    c.SerialNumber == bmsDetail.DeviceId ||
                    c.Name.Contains(bmsDetail.ChannelId.ToString()));

                battery.Packs.Add(new BatteryPackDto
                {
                    Id = matchingComponent?.Id.ToString() ?? bmsDetail.DeviceId ?? $"bms-{bmsDetail.ChannelId}",
                    Type = "battery",
                    Name = matchingComponent?.Name ?? $"Battery Pack {bmsDetail.ChannelId}",
                    Model = matchingComponent?.ModelNumber ?? "Generic",
                    Manufacturer = matchingComponent?.Manufacturer ?? "Ottomotive",
                    SerialNumber = matchingComponent?.SerialNumber ?? bmsDetail.DeviceId ?? "Generic",
                    StateOfCharge = bmsDetail.SocPercent
                });
            }
        }
        else
        {
            // Generic battery - use inverter-level SoC with first registered component
            var avgSoc = socState.InverterBatChannelSoc?
                .Where(c => c.SocPercent.HasValue)
                .Average(c => c.SocPercent!.Value);

            var mainComponent = components.First();
            battery.Id = mainComponent.Id;
            battery.Name = mainComponent.Name;
            battery.Model = mainComponent.ModelNumber ?? "Generic";
            battery.Manufacturer = mainComponent.Manufacturer ?? "Ottomotive";
            battery.SerialNumber = mainComponent.SerialNumber ?? "Generic";
            battery.StateOfCharge = avgSoc;

            // Add single pack with main component info
            battery.Packs.Add(new BatteryPackDto
            {
                Id = mainComponent.Id.ToString(),
                Type = "battery",
                Name = mainComponent.Name,
                Model = mainComponent.ModelNumber ?? "Generic",
                Manufacturer = mainComponent.Manufacturer ?? "Ottomotive",
                SerialNumber = mainComponent.SerialNumber ?? "Generic",
                StateOfCharge = avgSoc
            });
        }

        return battery;
    }

    /// <summary>
    /// Creates generic battery component(s) when not registered in DB
    /// </summary>
    private async Task<BatterySummaryDto> CreateGenericBatterySummaryAsync(
        Guid tbDeviceId,
        BatterySocStateDto socState,
        bool hasDetailedBmsData,
        bool hasInverterLevelData,
        CancellationToken cancellationToken)
    {
        var battery = new BatterySummaryDto
        {
            Type = "battery"
        };

        // Get the device from ThingsBoard mapping
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.ThingsBoardDeviceId == tbDeviceId, cancellationToken);

        if (device == null)
        {
            _logger.LogWarning("Device not found for ThingsBoard ID {TbDeviceId}", tbDeviceId);
            return battery;
        }

        if (hasDetailedBmsData && socState.BmsSocDetails != null && socState.BmsSocDetails.Any())
        {
            // Create generic BMS component for detailed battery data
            var allSocs = socState.BmsSocDetails
                .Where(d => d.SocPercent.HasValue)
                .Select(d => d.SocPercent!.Value)
                .ToList();

            var mainComponent = new Domain.Entities.DeviceComponent
            {
                Id = Guid.NewGuid(),
                DeviceId = device.Id,
                DeviceType = DeviceType.BMS,
                Name = "Battery Management System",
                Manufacturer = "Ottomotive",
                ModelNumber = "Generic",
                SerialNumber = socState.BmsSocDetails.First().DeviceId ?? "Generic",
                Specifications = JsonDocument.Parse("{}"),
                IsDeleted = false
            };

            _context.DeviceComponents.Add(mainComponent);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Created generic BMS component {ComponentId} for device {DeviceId} from RPC data",
                mainComponent.Id, device.Id);

            battery.Id = mainComponent.Id;
            battery.Name = mainComponent.Name;
            battery.Model = mainComponent.ModelNumber ?? "Generic";
            battery.Manufacturer = mainComponent.Manufacturer ?? "Ottomotive";
            battery.SerialNumber = mainComponent.SerialNumber ?? "Generic";
            battery.StateOfCharge = allSocs.Any() ? allSocs.Average() : null;

            // Add packs based on BMS details
            foreach (var bmsDetail in socState.BmsSocDetails)
            {
                battery.Packs.Add(new BatteryPackDto
                {
                    Id = bmsDetail.DeviceId ?? $"bms-{bmsDetail.ChannelId}",
                    Type = "battery",
                    Name = $"Battery Pack {bmsDetail.ChannelId}",
                    Model = "Generic",
                    Manufacturer = "Ottomotive",
                    SerialNumber = bmsDetail.DeviceId ?? "Generic",
                    StateOfCharge = bmsDetail.SocPercent
                });
            }
        }
        else if (hasInverterLevelData)
        {
            // Create generic battery component for inverter-level data
            var avgSoc = socState.InverterBatChannelSoc?
                .Where(c => c.SocPercent.HasValue)
                .Average(c => c.SocPercent!.Value);

            var bmsComponent = new Domain.Entities.DeviceComponent
            {
                Id = Guid.NewGuid(),
                DeviceId = device.Id,
                DeviceType = DeviceType.BMS,
                Name = "Generic Battery",
                Manufacturer = "Ottomotive",
                ModelNumber = "Generic",
                SerialNumber = "Generic",
                Specifications = JsonDocument.Parse("{}"),
                IsDeleted = false
            };

            _context.DeviceComponents.Add(bmsComponent);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Created generic battery component {ComponentId} for device {DeviceId} from RPC data",
                bmsComponent.Id, device.Id);

            battery.Id = bmsComponent.Id;
            battery.Name = bmsComponent.Name;
            battery.Model = bmsComponent.ModelNumber ?? "Generic";
            battery.Manufacturer = bmsComponent.Manufacturer ?? "Ottomotive";
            battery.SerialNumber = bmsComponent.SerialNumber ?? "Generic";
            battery.StateOfCharge = avgSoc;

            // Add single pack
            battery.Packs.Add(new BatteryPackDto
            {
                Id = bmsComponent.Id.ToString(),
                Type = "battery",
                Name = bmsComponent.Name,
                Model = bmsComponent.ModelNumber ?? "Generic",
                Manufacturer = bmsComponent.Manufacturer ?? "Ottomotive",
                SerialNumber = bmsComponent.SerialNumber ?? "Generic",
                StateOfCharge = avgSoc
            });
        }

        return battery;
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
                cancellationToken);

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
