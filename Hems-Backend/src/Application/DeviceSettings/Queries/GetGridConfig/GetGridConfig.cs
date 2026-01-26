using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.DeviceSettings.Queries.GetGridConfig;

/// <summary>
/// Query to get grid configuration parameters for a device.
/// Returns only grid-related parameters that cause device restart when changed.
/// </summary>
public record GetGridConfigQuery : IRequest<GetGridConfigResult>
{
    public Guid DeviceId { get; init; }
}

public class GetGridConfigQueryHandler : IRequestHandler<GetGridConfigQuery, GetGridConfigResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IDeviceLockService _deviceLockService;
    private readonly ILogger<GetGridConfigQueryHandler> _logger;

    public GetGridConfigQueryHandler(
        IApplicationDbContext context,
        IDeviceLockService deviceLockService,
        ILogger<GetGridConfigQueryHandler> logger)
    {
        _context = context;
        _deviceLockService = deviceLockService;
        _logger = logger;
    }

    public async Task<GetGridConfigResult> Handle(GetGridConfigQuery request, CancellationToken cancellationToken)
    {
        // Check if device is restarting
        if (_deviceLockService.IsDeviceRestarting(request.DeviceId))
        {
            var restartInfo = _deviceLockService.GetRestartInfo(request.DeviceId);
            return new GetGridConfigResult
            {
                Success = false,
                ErrorCode = "DEVICE_RESTARTING",
                ErrorMessage = "Device is restarting. Please wait and try again.",
                EstimatedAvailableAt = restartInfo?.EstimatedAvailableAt
            };
        }

        // Find SetupDevice by DeviceId
        var setupDevice = await _context.SetupDevices
            .FirstOrDefaultAsync(sd => sd.DeviceId == request.DeviceId, cancellationToken);

        if (setupDevice == null)
        {
            return new GetGridConfigResult
            {
                Success = false,
                ErrorCode = "DEVICE_NOT_FOUND",
                ErrorMessage = "Device not found"
            };
        }

        // Get all grid config parameters for this device
        var allParameterValues = await _context.SetupDeviceParameterValues
            .Include(sdpv => sdpv.Parameter)
            .Include(sdpv => sdpv.ParameterGroup)
            .Where(sdpv => sdpv.SetupDeviceId == setupDevice.Id)
            .ToListAsync(cancellationToken);

        // Filter to only grid configuration parameters
        var gridConfigParameters = allParameterValues
            .Where(pv => pv.Parameter != null && RpcMethodMapping.IsGridConfigParameter(pv.Parameter.Name))
            .Select(pv => new GridConfigParameterDto
            {
                ParameterId = pv.ParameterId,
                ParameterName = pv.Parameter!.Name,
                ParameterDescription = pv.Parameter.Description ?? string.Empty,
                Value = pv.Value,
                Unit = pv.Parameter.Unit ?? string.Empty,
                ElementType = pv.Parameter.ElementType ?? "string",
                ParameterGroupId = pv.ParameterGroupId,
                ParameterGroupName = pv.ParameterGroup?.Name,
                LastModified = pv.LastModified
            })
            .OrderBy(p => p.ParameterGroupName)
            .ThenBy(p => p.ParameterName)
            .ToList();

        _logger.LogInformation(
            "Retrieved {Count} grid config parameters for device {DeviceId}",
            gridConfigParameters.Count, request.DeviceId);

        return new GetGridConfigResult
        {
            Success = true,
            DeviceId = request.DeviceId,
            Parameters = gridConfigParameters
        };
    }
}

/// <summary>
/// Result of GetGridConfig query
/// </summary>
public record GetGridConfigResult
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid DeviceId { get; init; }
    public List<GridConfigParameterDto> Parameters { get; init; } = new();

    /// <summary>
    /// When device is restarting, the estimated time when it will be available.
    /// </summary>
    public DateTime? EstimatedAvailableAt { get; init; }
}

/// <summary>
/// DTO for a grid configuration parameter
/// </summary>
public record GridConfigParameterDto
{
    public Guid ParameterId { get; init; }
    public string ParameterName { get; init; } = string.Empty;
    public string ParameterDescription { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public string ElementType { get; init; } = string.Empty;
    public Guid? ParameterGroupId { get; init; }
    public string? ParameterGroupName { get; init; }
    public DateTimeOffset? LastModified { get; init; }
}
