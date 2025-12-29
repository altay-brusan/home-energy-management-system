using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.DeviceSettings.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceSettings.Queries.GetCurrentDeviceMode;

public record GetCurrentDeviceModeQuery : IRequest<GetCurrentDeviceModeResult>
{
    public Guid DeviceId { get; init; }
}

public class GetCurrentDeviceModeQueryHandler : IRequestHandler<GetCurrentDeviceModeQuery, GetCurrentDeviceModeResult>
{
    private readonly IApplicationDbContext _context;

    public GetCurrentDeviceModeQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GetCurrentDeviceModeResult> Handle(GetCurrentDeviceModeQuery request, CancellationToken cancellationToken)
    {
        // Find the device first
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.Id == request.DeviceId && !d.IsDeleted, cancellationToken);

        if (device == null)
        {
            return new GetCurrentDeviceModeResult
            {
                Success = false,
                ErrorMessage = "Device not found"
            };
        }

        // Get the latest active mode for this device
        var currentMode = await _context.DeviceModeHistory
            .Where(m => m.DeviceId == request.DeviceId && m.IsActive)
            .OrderByDescending(m => m.Created)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentMode == null)
        {
            return new GetCurrentDeviceModeResult
            {
                Success = true,
                Mode = null,
                ModeDescription = null,
                Message = "No mode has been set for this device"
            };
        }

        var mode = (DeviceMode)currentMode.Mode;
        var modeInfo = DeviceModeConfigurations.GetAllModesWithDescriptions()
            .GetValueOrDefault(mode);

        return new GetCurrentDeviceModeResult
        {
            Success = true,
            Mode = mode,
            ModeDescription = currentMode.Description ?? modeInfo?.Name,
            LastModified = currentMode.LastModified
        };
    }
}

/// <summary>
/// Result of getting current device mode
/// </summary>
public record GetCurrentDeviceModeResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Message { get; init; }
    public DeviceMode? Mode { get; init; }
    public string? ModeDescription { get; init; }
    public DateTimeOffset? LastModified { get; init; }
}
