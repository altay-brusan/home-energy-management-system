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
            identity = await GetInverterIdentityFromRpcAsync(tbDeviceId, cancellationToken);

            // If we got identity from RPC and no component exists, we could create a generic one
            // For now, we'll just use the RPC data
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

        // Get active power from inverter state
        var state = await GetInverterStateFromRpcAsync(tbDeviceId, cancellationToken);
        if (state?.GridSideActivePowerW != null)
        {
            inverter.ActivePowerKw = state.GridSideActivePowerW / 1000.0;
        }

        // Store additional properties in component if needed
        if (identity?.AdditionalProperties != null && identity.AdditionalProperties.Any() && component != null)
        {
            // TODO: Update component.Specifications with additional properties
        }

        return inverter;
    }

    private async Task<InverterIdentityDto?> GetInverterIdentityFromRpcAsync(
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

    private async Task<InverterDeviceStateDto?> GetInverterStateFromRpcAsync(
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
        // Get SoC state from device
        var socState = await GetBatterySocStateFromRpcAsync(tbDeviceId, cancellationToken);

        if (socState == null)
        {
            // No battery data available
            return null;
        }

        var battery = new BatterySummaryDto
        {
            Id = Guid.NewGuid(),
            Type = "battery"
        };

        // Check if we have detailed BMS data or just inverter-level data
        var hasDetailedBmsData = socState.BmsSocDetails != null && socState.BmsSocDetails.Any();
        var hasGenericBattery = !hasDetailedBmsData &&
                               socState.InverterBatChannelSoc != null &&
                               socState.InverterBatChannelSoc.Any(c => c.SocPercent.HasValue);

        if (hasGenericBattery)
        {
            // Generic battery - use inverter-level SoC
            var avgSoc = socState.InverterBatChannelSoc?
                .Where(c => c.SocPercent.HasValue)
                .Average(c => c.SocPercent!.Value);

            battery.Name = "Generic Battery";
            battery.Model = "Generic";
            battery.Manufacturer = "Ottomotive";
            battery.SerialNumber = "Generic";
            battery.StateOfCharge = avgSoc;

            // Add the same data to packs[0] as requested
            battery.Packs.Add(new BatteryPackDto
            {
                Id = "generic-pack-1",
                Type = "battery",
                Name = "Generic Battery",
                Model = "Generic",
                Manufacturer = "Ottomotive",
                SerialNumber = "Generic",
                StateOfCharge = avgSoc
            });
        }
        else if (hasDetailedBmsData)
        {
            // We have detailed BMS data - create individual packs
            var allSocs = socState.BmsSocDetails!
                .Where(d => d.SocPercent.HasValue)
                .Select(d => d.SocPercent!.Value)
                .ToList();

            battery.Name = "Battery Management System";
            battery.Model = components.FirstOrDefault()?.ModelNumber ?? "Generic";
            battery.Manufacturer = components.FirstOrDefault()?.Manufacturer ?? "Ottomotive";
            battery.SerialNumber = components.FirstOrDefault()?.SerialNumber ?? "Generic";
            battery.StateOfCharge = allSocs.Any() ? allSocs.Average() : null;

            foreach (var bmsDetail in socState.BmsSocDetails!)
            {
                var component = components.FirstOrDefault(c => c.SerialNumber == bmsDetail.DeviceId);

                battery.Packs.Add(new BatteryPackDto
                {
                    Id = bmsDetail.DeviceId ?? $"bms-{bmsDetail.ChannelId}",
                    Type = "battery",
                    Name = component?.Name ?? $"Battery Pack {bmsDetail.ChannelId}",
                    Model = component?.ModelNumber ?? "Generic",
                    Manufacturer = component?.Manufacturer ?? "Ottomotive",
                    SerialNumber = component?.SerialNumber ?? bmsDetail.DeviceId ?? "Generic",
                    StateOfCharge = bmsDetail.SocPercent
                });
            }
        }
        else
        {
            // No battery or battery is dead
            return null;
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
