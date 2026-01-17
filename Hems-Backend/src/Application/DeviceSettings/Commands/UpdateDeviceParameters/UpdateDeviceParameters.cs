using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.DeviceSettings.Commands.UpdateDeviceParameters;

public record UpdateDeviceParametersCommand : IRequest<UpdateDeviceParametersResult>
{
    public Guid DeviceId { get; init; }
    public List<ParameterUpdate> Parameters { get; init; } = new();
}

public record ParameterUpdate
{
    public Guid ParameterId { get; init; }
    public string Value { get; init; } = string.Empty;
}

public class UpdateDeviceParametersCommandHandler : IRequestHandler<UpdateDeviceParametersCommand, UpdateDeviceParametersResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IDeviceParameterUpdateService _updateService;
    private readonly IDeviceLockService _deviceLockService;
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUser;
    private readonly ILogger<UpdateDeviceParametersCommandHandler> _logger;

    public UpdateDeviceParametersCommandHandler(
        IApplicationDbContext context,
        IDeviceParameterUpdateService updateService,
        IDeviceLockService deviceLockService,
        IIdentityService identityService,
        IUser currentUser,
        ILogger<UpdateDeviceParametersCommandHandler> logger)
    {
        _context = context;
        _updateService = updateService;
        _deviceLockService = deviceLockService;
        _identityService = identityService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<UpdateDeviceParametersResult> Handle(UpdateDeviceParametersCommand request, CancellationToken cancellationToken)
    {
        // DEBUG: Log exact request received from frontend
        _logger.LogInformation(
            "📥 Parameter update request received: DeviceId={DeviceId}, ParameterCount={Count}, Parameters=[{Parameters}]",
            request.DeviceId,
            request.Parameters.Count,
            string.Join("; ", request.Parameters.Select(p => $"Id={p.ParameterId}, Value='{p.Value}'")));

        if (request.Parameters.Count == 0)
        {
            _logger.LogWarning("❌ Parameter update rejected: No parameters provided");
            return new UpdateDeviceParametersResult
            {
                Success = false,
                ErrorMessage = "No parameters provided"
            };
        }

        // Find SetupDevice by DeviceId
        var setupDevice = await _context.SetupDevices
            .Include(sd => sd.Device)
            .FirstOrDefaultAsync(sd => sd.DeviceId == request.DeviceId, cancellationToken);

        if (setupDevice?.Device == null)
        {
            return new UpdateDeviceParametersResult
            {
                Success = false,
                ErrorMessage = "Device not found"
            };
        }

        // Check if device is currently locked (immediate feedback)
        if (_deviceLockService.IsDeviceLocked(request.DeviceId))
        {
            _logger.LogWarning(
                "Device {DeviceId} is currently locked - another update is in progress",
                request.DeviceId);

            return new UpdateDeviceParametersResult
            {
                Success = false,
                ErrorMessage = "Another update operation is in progress for this device. Please wait and try again.",
                ErrorCode = "DEVICE_BUSY"
            };
        }

        // Get all parameter values for this device
        var parameterIds = request.Parameters.Select(p => p.ParameterId).ToList();

        _logger.LogInformation(
            "🔍 Looking up {Count} parameters for SetupDeviceId {SetupDeviceId}: [{ParameterIds}]",
            parameterIds.Count,
            setupDevice.Id,
            string.Join(", ", parameterIds));

        var parameterValues = await _context.SetupDeviceParameterValues
            .Include(sdpv => sdpv.Parameter)
            .Where(sdpv =>
                sdpv.SetupDeviceId == setupDevice.Id &&
                parameterIds.Contains(sdpv.ParameterId))
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "📋 Found {FoundCount}/{RequestedCount} parameter values for device {DeviceId}. Found: [{FoundParams}]",
            parameterValues.Count,
            parameterIds.Count,
            request.DeviceId,
            string.Join("; ", parameterValues.Select(pv => $"{pv.Parameter?.Name ?? "null"}={pv.Value}")));

        if (parameterValues.Count == 0)
        {
            _logger.LogWarning(
                "No matching parameters found for device {DeviceId}. Requested parameter IDs: {ParameterIds}",
                request.DeviceId,
                string.Join(", ", parameterIds));

            return new UpdateDeviceParametersResult
            {
                Success = false,
                ErrorMessage = $"No matching parameters found for this device. Requested {parameterIds.Count} parameter(s) but none matched."
            };
        }

        var device = setupDevice.Device;

        // RBAC: Check if user is admin (needed for admin-only parameters)
        bool isAdmin = false;
        if (_currentUser.Id != null)
        {
            isAdmin = await _identityService.IsInRoleAsync(_currentUser.Id, Roles.Administrator);
        }

        // Validate parameters and check authorization
        var validParameters = new List<ParameterUpdateItem>();
        var unauthorizedParameters = new List<string>();
        var notFoundParameters = new List<Guid>();

        foreach (var update in request.Parameters)
        {
            var paramValue = parameterValues.FirstOrDefault(pv => pv.ParameterId == update.ParameterId);
            if (paramValue?.Parameter == null)
            {
                notFoundParameters.Add(update.ParameterId);
                continue;
            }

            // RBAC: Check if user has permission to update this parameter
            if (paramValue.Parameter.RoleType == "Admin" && !isAdmin)
            {
                unauthorizedParameters.Add(paramValue.Parameter.Name);
                _logger.LogWarning(
                    "User {UserId} attempted to update admin-only parameter {ParameterName}",
                    _currentUser.Id,
                    paramValue.Parameter.Name);
                continue;
            }

            validParameters.Add(new ParameterUpdateItem
            {
                ParameterId = update.ParameterId,
                ParameterName = paramValue.Parameter.Name,
                Value = update.Value
            });
        }

        // If there are unauthorized parameters, return error
        if (unauthorizedParameters.Count > 0)
        {
            return new UpdateDeviceParametersResult
            {
                Success = false,
                ErrorMessage = $"Access denied for {unauthorizedParameters.Count} admin-only parameter(s): {string.Join(", ", unauthorizedParameters)}"
            };
        }

        // If some parameters were not found, log and return error with details
        if (notFoundParameters.Count > 0)
        {
            _logger.LogWarning(
                "Parameter update request contained {NotFoundCount} invalid parameter IDs: {ParameterIds}",
                notFoundParameters.Count,
                string.Join(", ", notFoundParameters));

            // If ALL parameters were not found, return error
            if (validParameters.Count == 0)
            {
                return new UpdateDeviceParametersResult
                {
                    Success = false,
                    ErrorMessage = $"No parameters found. {notFoundParameters.Count} parameter ID(s) were invalid for this device.",
                    FailedParameters = notFoundParameters.Select(id => new FailedParameterInfo
                    {
                        ParameterId = id,
                        ParameterName = "Unknown",
                        ErrorMessage = "Parameter not found for this device"
                    }).ToList()
                };
            }
        }

        // If no parameters left after validation
        if (validParameters.Count == 0)
        {
            _logger.LogWarning("❌ No valid parameters to update after validation");
            return new UpdateDeviceParametersResult
            {
                Success = false,
                ErrorMessage = "No valid parameters to update"
            };
        }

        _logger.LogInformation(
            "✅ Validated {Count} parameters for update: [{Parameters}]",
            validParameters.Count,
            string.Join("; ", validParameters.Select(p => $"{p.ParameterName}='{p.Value}'")));

        try
        {
            // Execute synchronous update - this waits for completion
            _logger.LogInformation("🚀 Starting parameter update via UpdateService for device {DeviceId}", request.DeviceId);
            var updateResult = await _updateService.UpdateParametersAsync(
                request.DeviceId,
                setupDevice.Id,
                device.ThingsBoardDeviceId,
                validParameters,
                cancellationToken);

            _logger.LogInformation(
                "Parameter update completed for device {DeviceId}: {Success}/{Total} succeeded, {Failed} failed in {Duration}ms",
                request.DeviceId,
                updateResult.SuccessCount,
                updateResult.TotalParameters,
                updateResult.FailedCount,
                updateResult.Duration.TotalMilliseconds);

            return new UpdateDeviceParametersResult
            {
                Success = updateResult.Success,
                TotalParameters = updateResult.TotalParameters,
                SuccessCount = updateResult.SuccessCount,
                FailedCount = updateResult.FailedCount,
                FailedParameters = updateResult.FailedParameters.Select(fp => new FailedParameterInfo
                {
                    ParameterId = fp.ParameterId,
                    ParameterName = fp.ParameterName,
                    ErrorMessage = fp.ErrorMessage
                }).ToList(),
                Duration = updateResult.Duration
            };
        }
        catch (DeviceBusyException)
        {
            return new UpdateDeviceParametersResult
            {
                Success = false,
                ErrorCode = "DEVICE_BUSY",
                ErrorMessage = "Device became busy during operation. Please wait and try again."
            };
        }
        catch (OperationCanceledException)
        {
            return new UpdateDeviceParametersResult
            {
                Success = false,
                ErrorCode = "TIMEOUT",
                ErrorMessage = "Operation timed out."
            };
        }
    }
}

/// <summary>
/// Result of batch parameter update - complete synchronous result
/// </summary>
public record UpdateDeviceParametersResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Total number of parameters in the request
    /// </summary>
    public int TotalParameters { get; init; }

    /// <summary>
    /// Number of parameters successfully updated
    /// </summary>
    public int SuccessCount { get; init; }

    /// <summary>
    /// Number of parameters that failed to update
    /// </summary>
    public int FailedCount { get; init; }

    /// <summary>
    /// Details of failed parameters
    /// </summary>
    public List<FailedParameterInfo> FailedParameters { get; init; } = new();

    /// <summary>
    /// Total duration of the operation
    /// </summary>
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Information about a failed parameter update
/// </summary>
public record FailedParameterInfo
{
    public Guid ParameterId { get; init; }
    public string ParameterName { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
}
