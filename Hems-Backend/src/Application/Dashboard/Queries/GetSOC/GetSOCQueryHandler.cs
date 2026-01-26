using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Hems_Backend.Application.Dashboard.Queries.GetSoc;

public class GetSocQueryHandler : IRequestHandler<GetSocQuery, SocResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly IDeviceMappingService _deviceMapping;
    private readonly IDemoDataService _demoDataService;
    private readonly IDemoSimulationDataService _demoSimulationService;

    public GetSocQueryHandler(
        IApplicationDbContext context,
        ISender sender,
        IDeviceMappingService deviceMapping,
        IDemoDataService demoDataService,
        IDemoSimulationDataService demoSimulationService)
    {
        _context = context;
        _sender = sender;
        _deviceMapping = deviceMapping;
        _demoDataService = demoDataService;
        _demoSimulationService = demoSimulationService;
    }

    public async Task<SocResponseDto> Handle(GetSocQuery request, CancellationToken cancellationToken)
    {
        // Device access check (uses local DeviceId)
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Device access denied");
        }

        // Check if demo device - return simulated SOC from centralized service
        var isDemoDevice = await _demoDataService.IsDemoDeviceAsync(request.DeviceId, cancellationToken);
        if (isDemoDevice)
        {
            var demoData = _demoSimulationService.GetDemoSocResponse(request.DeviceId);
            return new SocResponseDto
            {
                TimestampUtc = demoData.TimestampUtc,
                Version = demoData.Version,
                DeviceId = request.DeviceId,
                InverterBatChannelSoc = demoData.InverterBatChannelSoc.Select(c => new InverterBatChannelSocDto
                {
                    ChannelId = c.ChannelId,
                    socPercent = c.SocPercent
                }).ToList(),
                bmsSocDetails = demoData.BmsSocDetails.Select(b => new BmsSocDetailDto
                {
                    ChannelId = b.ChannelId,
                    DeviceId = b.DeviceId,
                    socPercent = b.SocPercent
                }).ToList()
            };
        }

        // Map local DeviceId to ThingsBoardDeviceId for querying telemetry data
        var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(request.DeviceId, cancellationToken);
        if (thingsBoardDeviceId == null)
        {
            throw new ArgumentException($"Device with ID {request.DeviceId} not found or has no ThingsBoard mapping.");
        }

        // Query the latest BatterySoc data for the device using ThingsBoardDeviceId
        var latestSocData = await _context.DeviceData
            .Where(dd => dd.ThingsBoardDeviceId == thingsBoardDeviceId && dd.DataType == "BatterySoc")
            .OrderByDescending(dd => dd.DeviceLocalTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestSocData == null)
        {
            // Return empty response if no data found
            return new SocResponseDto
            {
                TimestampUtc = DateTime.UtcNow.ToString("o"),
                Version = 0,
                DeviceId = request.DeviceId,
                InverterBatChannelSoc = new List<InverterBatChannelSocDto>(),
                bmsSocDetails = new List<BmsSocDetailDto>()
            };
        }

        // Parse the JSON data
        var jsonData = latestSocData.Data.RootElement;

        // Extract timestampUtc
        var timestampUtc = jsonData.TryGetProperty("timestampUtc", out var tsElement)
            ? tsElement.GetString() ?? DateTime.UtcNow.ToString("o")
            : DateTime.UtcNow.ToString("o");

        // Extract version
        var version = jsonData.TryGetProperty("version", out var versionElement)
            ? versionElement.GetInt32()
            : 0;

        // Extract inverterBatChannelSoc array
        var inverterChannels = new List<InverterBatChannelSocDto>();
        if (jsonData.TryGetProperty("inverterBatChannelSoc", out var inverterArrayElement)
            && inverterArrayElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in inverterArrayElement.EnumerateArray())
            {
                var channelId = item.TryGetProperty("channelId", out var chIdEl) ? chIdEl.GetInt32() : 0;
                var socPercent = item.TryGetProperty("socPercent", out var socEl) ? socEl.GetDouble() : 0.0;

                inverterChannels.Add(new InverterBatChannelSocDto
                {
                    ChannelId = channelId,
                    socPercent = socPercent
                });
            }
        }

        // Extract bmsSocDetails array
        var bmsDetails = new List<BmsSocDetailDto>();
        if (jsonData.TryGetProperty("bmsSocDetails", out var bmsArrayElement)
            && bmsArrayElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in bmsArrayElement.EnumerateArray())
            {
                var channelId = item.TryGetProperty("channelId", out var chIdEl) ? chIdEl.GetInt32() : 0;
                var deviceId = item.TryGetProperty("deviceId", out var devIdEl) ? devIdEl.GetString() ?? string.Empty : string.Empty;
                var socPercent = item.TryGetProperty("socPercent", out var socEl) ? socEl.GetDouble() : 0.0;

                bmsDetails.Add(new BmsSocDetailDto
                {
                    ChannelId = channelId,
                    DeviceId = deviceId,
                    socPercent = socPercent
                });
            }
        }

        return new SocResponseDto
        {
            TimestampUtc = timestampUtc,
            Version = version,
            DeviceId = request.DeviceId,
            InverterBatChannelSoc = inverterChannels,
            bmsSocDetails = bmsDetails
        };
    }
}
