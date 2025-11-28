using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Hems_Backend.Application.Devices.Queries.GetDeviceWithData;

public class GetDeviceWithDataQueryHandler : IRequestHandler<GetDeviceWithDataQuery, DeviceWithDataResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly IDeviceMappingService _deviceMapping;

    public GetDeviceWithDataQueryHandler(IApplicationDbContext context, ISender sender, IDeviceMappingService deviceMapping)
    {
        _context = context;
        _sender = sender;
        _deviceMapping = deviceMapping;
    }

    public async Task<DeviceWithDataResponseDto> Handle(GetDeviceWithDataQuery request, CancellationToken cancellationToken)
    {
        // Device access kontrolü
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Device access denied");
        }

        // Map local DeviceId to ThingsBoardDeviceId
        var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(request.DeviceId, cancellationToken);
        if (thingsBoardDeviceId == null)
        {
            throw new ArgumentException($"Device with ID {request.DeviceId} not found or has no ThingsBoard mapping.");
        }

        // Device ve ilişkili verileri al
        var device = await _context.Devices
            .Include(d => d.SetupDevice!)
            .ThenInclude(sd => sd.Setup!)
            .ThenInclude(s => s.Site!)
            .Include(d => d.DeviceComponents)
            .FirstOrDefaultAsync(d => d.Id == request.DeviceId && !d.IsDeleted, cancellationToken);

        if (device == null)
        {
            throw new ArgumentException($"Device with ID {request.DeviceId} not found.");
        }

        var components = new List<ComponentDto>();

        foreach (var deviceComponent in device.DeviceComponents)
        {
            object? componentData = null;

            switch (deviceComponent.DeviceType)
            {
                case DeviceType.BMS:
                    componentData = await GetBMSDataAsync(thingsBoardDeviceId.Value, cancellationToken);
                    break;
                case DeviceType.Battery:
                    componentData = await GetBatteryDataAsync(deviceComponent.SerialNumber ?? "", cancellationToken);
                    break;
                case DeviceType.Invertor:
                    componentData = await GetInverterDataAsync(thingsBoardDeviceId.Value, cancellationToken);
                    break;
            }

            components.Add(new ComponentDto
            {
                ComponentId = deviceComponent.Id,
                ComponentName = deviceComponent.Name,
                ComponentDeviceType = deviceComponent.DeviceType.ToString(),
                SerialNumber = deviceComponent.SerialNumber ?? "",
                ComponentData = componentData
            });
        }

        return new DeviceWithDataResponseDto
        {
            SiteId = device.SetupDevice?.Setup?.Site?.Id ?? Guid.Empty,
            SetupId = device.SetupDevice?.Setup?.Id ?? Guid.Empty,
            DeviceId = device.Id,
            ModelNumber = device.ModelNumber ?? "",
            DeviceManufacturer = device.Manufacturer ?? "",
            Components = components
        };
    }

    private async Task<object?> GetBMSDataAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken)
    {
        var batteryData = await _context.DeviceData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId && x.DataType == "BatteryClusterData")
            .OrderByDescending(x => x.RequestDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (batteryData == null) return null;

        try
        {
            // Direkt SOC değerini al, JSON parse etme
            return batteryData.Data.RootElement.GetProperty("SOC").GetDouble();
        }
        catch
        {
            return null;
        }
    }

    private async Task<object?> GetBatteryDataAsync(string serialNumber, CancellationToken cancellationToken)
    {
        var batteryData = await _context.DeviceData
            .Where(x => x.DataType == "BatteryData")
            .OrderByDescending(x => x.RequestDate)
            .ToListAsync(cancellationToken);

        foreach (var data in batteryData)
        {
            try
            {
                // Direkt JSON element'leri kontrol et
                if (data.Data.RootElement.TryGetProperty("DeviceSerialNumber", out var sn) && 
                    sn.GetString() == serialNumber)
                {
                    if (data.Data.RootElement.TryGetProperty("SOCCumulative", out var socCumulative))
                    {
                        var socArray = socCumulative.EnumerateArray()
                            .Select(x => x.GetDouble())
                            .ToList();
                        
                        return socArray.Any() ? socArray.Average() : null;
                    }
                    return null;
                }
            }
            catch
            {
                continue;
            }
        }
        
        return null;
    }

    private async Task<object?> GetInverterDataAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken)
    {
        var energyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId)
            .OrderByDescending(x => x.DataDateTime)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (energyData.Count < 2) return null;

        var current = energyData[0];
        var previous = energyData[1];

        var currentValue = (current.LoadEnergyKWh ?? 0) + (current.BatteryChargeEnergyKWh ?? 0);
        var previousValue = (previous.LoadEnergyKWh ?? 0) + (previous.BatteryChargeEnergyKWh ?? 0);

        return currentValue - previousValue;
    }
}
