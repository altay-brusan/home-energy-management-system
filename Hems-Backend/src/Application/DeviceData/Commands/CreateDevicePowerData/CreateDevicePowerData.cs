using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceData.Commands.CreateDevicePowerData;

public record CreateDevicePowerDataCommand : IRequest<int>
{
    public string DataType { get; init; } = string.Empty;
    public Guid DeviceId { get; init; }
    public DateTime DeviceLocalTime { get; init; }
    public int? Version { get; init; }
    
    // PV Power (W)
    public double? PvTotalPowerW { get; init; }
    public double? PvMppt1PowerW { get; init; }
    public double? PvMppt2PowerW { get; init; }
    public double? PvMppt3PowerW { get; init; }
    public double? PvMppt4PowerW { get; init; }
    
    // Grid Power (W) - Positive = import, Negative = export
    public double? GridTotalPowerW { get; init; }
    
    // Load Power (W)
    public double? LoadTotalPowerW { get; init; }
    
    // Battery Power (W) - Positive = discharge, Negative = charge
    public double? BatteryPowerW { get; init; }
    
    // Generator Power (W)
    public double? GeneratorTotalPowerW { get; init; }
}

public class CreateDevicePowerDataCommandHandler : IRequestHandler<CreateDevicePowerDataCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ITimezoneService _timezoneService;
    private readonly IDeviceMappingService _deviceMapping;

    public CreateDevicePowerDataCommandHandler(
        IApplicationDbContext context,
        ITimezoneService timezoneService,
        IDeviceMappingService deviceMapping)
    {
        _context = context;
        _timezoneService = timezoneService;
        _deviceMapping = deviceMapping;
    }

    public async Task<int> Handle(CreateDevicePowerDataCommand request, CancellationToken cancellationToken)
    {
        // Check if device exists (DeviceId is actually ThingsBoardDeviceId)
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.ThingsBoardDeviceId == request.DeviceId && !d.IsDeleted, cancellationToken);

        if (device == null)
        {
            throw new ArgumentException($"Device with ThingsBoardDeviceId {request.DeviceId} not found.");
        }

        // Convert UTC DeviceLocalTime to device local time using cached timezone service
        var localDeviceId = await _deviceMapping.GetDeviceIdAsync(request.DeviceId, cancellationToken);
        if (localDeviceId == null)
        {
            throw new ArgumentException($"Device mapping not found for ThingsBoardDeviceId {request.DeviceId}");
        }
        
        var localDeviceLocalTime = await _timezoneService.ConvertUtcToDeviceTimeAsync(
            localDeviceId.Value, 
            request.DeviceLocalTime, 
            cancellationToken);

        // Define duplicate detection threshold (in seconds)
        const int duplicateThresholdSeconds = 30; // Consider messages within 30 seconds as potential duplicates

        // Check for exact match first (most common case) - using local time
        var exactMatch = await _context.DevicePowerData
            .FirstOrDefaultAsync(x =>
                x.ThingsBoardDeviceId == request.DeviceId &&
                x.DeviceLocalTime == localDeviceLocalTime &&
                x.DataType == request.DataType,
                cancellationToken);

        if (exactMatch != null)
        {
            // Update existing record with exact timestamp
            exactMatch.Version = request.Version;
            exactMatch.PvTotalPowerW = request.PvTotalPowerW;
            exactMatch.PvMppt1PowerW = request.PvMppt1PowerW;
            exactMatch.PvMppt2PowerW = request.PvMppt2PowerW;
            exactMatch.PvMppt3PowerW = request.PvMppt3PowerW;
            exactMatch.PvMppt4PowerW = request.PvMppt4PowerW;
            exactMatch.GridTotalPowerW = request.GridTotalPowerW;
            exactMatch.LoadTotalPowerW = request.LoadTotalPowerW;
            exactMatch.BatteryPowerW = request.BatteryPowerW;
            exactMatch.GeneratorTotalPowerW = request.GeneratorTotalPowerW;
            exactMatch.LastModified = DateTime.UtcNow;
            exactMatch.LastModifiedBy = "DeviceSimulator";

            await _context.SaveChangesAsync(cancellationToken);
            return exactMatch.Id;
        }

        // Check for recent record with close timestamp (within threshold) - using local time
        var timeWindowStart = localDeviceLocalTime.AddSeconds(-duplicateThresholdSeconds);
        var timeWindowEnd = localDeviceLocalTime.AddSeconds(duplicateThresholdSeconds);

        var recentRecord = await _context.DevicePowerData
            .Where(x =>
                x.ThingsBoardDeviceId == request.DeviceId &&
                x.DataType == request.DataType &&
                x.DeviceLocalTime >= timeWindowStart &&
                x.DeviceLocalTime <= timeWindowEnd)
            .OrderByDescending(x => x.DeviceLocalTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (recentRecord != null)
        {
            // Found a duplicate within threshold - update the existing record instead of creating new
            // This handles cases where device sends slightly different timestamps
            recentRecord.Version = request.Version;
            recentRecord.PvTotalPowerW = request.PvTotalPowerW;
            recentRecord.PvMppt1PowerW = request.PvMppt1PowerW;
            recentRecord.PvMppt2PowerW = request.PvMppt2PowerW;
            recentRecord.PvMppt3PowerW = request.PvMppt3PowerW;
            recentRecord.PvMppt4PowerW = request.PvMppt4PowerW;
            recentRecord.GridTotalPowerW = request.GridTotalPowerW;
            recentRecord.LoadTotalPowerW = request.LoadTotalPowerW;
            recentRecord.BatteryPowerW = request.BatteryPowerW;
            recentRecord.GeneratorTotalPowerW = request.GeneratorTotalPowerW;

            // Update the timestamp to the new one if it's more recent
            // Update with newer timestamp (comparing local times)
            if (localDeviceLocalTime > recentRecord.DeviceLocalTime)
            {
                recentRecord.DeviceLocalTime = localDeviceLocalTime;
            }

            recentRecord.LastModified = DateTime.UtcNow;
            recentRecord.LastModifiedBy = "DeviceSimulator";

            await _context.SaveChangesAsync(cancellationToken);
            return recentRecord.Id;
        }

        // No duplicate found - create new record
        var powerData = new DevicePowerData
        {
            ThingsBoardDeviceId = request.DeviceId,
            DeviceLocalTime = localDeviceLocalTime, // Store in device local time
            DataType = request.DataType,
            MessageType = MessageTypes.Telemetry,
            PhaseType = PhaseTypes.SinglePhase,
            Version = request.Version,

            PvTotalPowerW = request.PvTotalPowerW,
            PvMppt1PowerW = request.PvMppt1PowerW,
            PvMppt2PowerW = request.PvMppt2PowerW,
            PvMppt3PowerW = request.PvMppt3PowerW,
            PvMppt4PowerW = request.PvMppt4PowerW,
            GridTotalPowerW = request.GridTotalPowerW,
            LoadTotalPowerW = request.LoadTotalPowerW,
            BatteryPowerW = request.BatteryPowerW,
            GeneratorTotalPowerW = request.GeneratorTotalPowerW,

            Created = DateTime.UtcNow,
            CreatedBy = "DeviceSimulator"
        };

        _context.DevicePowerData.Add(powerData);
        await _context.SaveChangesAsync(cancellationToken);

        return powerData.Id;
    }
}
