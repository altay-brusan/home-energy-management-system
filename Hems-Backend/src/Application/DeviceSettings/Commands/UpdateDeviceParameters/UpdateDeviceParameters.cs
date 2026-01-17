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
    private readonly IParameterUpdateQueueService _updateQueue;
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUser;
    private readonly ILogger<UpdateDeviceParametersCommandHandler> _logger;

    public UpdateDeviceParametersCommandHandler(
        IApplicationDbContext context,
        IParameterUpdateQueueService updateQueue,
        IIdentityService identityService,
        IUser currentUser,
        ILogger<UpdateDeviceParametersCommandHandler> logger)
    {
        _context = context;
        _updateQueue = updateQueue;
        _identityService = identityService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<UpdateDeviceParametersResult> Handle(UpdateDeviceParametersCommand request, CancellationToken cancellationToken)
    {
        if (request.Parameters.Count == 0)
        {
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

        // Get all parameter values for this device
        var parameterIds = request.Parameters.Select(p => p.ParameterId).ToList();
        var parameterValues = await _context.SetupDeviceParameterValues
            .Include(sdpv => sdpv.Parameter)
            .Where(sdpv =>
                sdpv.SetupDeviceId == setupDevice.Id &&
                parameterIds.Contains(sdpv.ParameterId))
            .ToListAsync(cancellationToken);

        if (parameterValues.Count == 0)
        {
            return new UpdateDeviceParametersResult
            {
                Success = false,
                ErrorMessage = "No matching parameters found for this device"
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
        var validParameters = new List<ParameterUpdateQueueItem>();
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

            validParameters.Add(new ParameterUpdateQueueItem
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

        // If no parameters left after validation
        if (validParameters.Count == 0)
        {
            return new UpdateDeviceParametersResult
            {
                Success = false,
                ErrorMessage = "No valid parameters to update"
            };
        }

        // Generate correlation ID for tracking this batch operation
        var correlationId = Guid.NewGuid().ToString("N");

        // IMPORTANT: Initialize state as "pending" BEFORE queuing
        // This ensures late-subscribing SignalR clients will find cached state
        // when they subscribe after the API returns but before processing starts
        _updateQueue.InitializeOperationState(correlationId, request.DeviceId, validParameters.Count);

        // Queue the update request for background processing
        await _updateQueue.QueueParameterUpdateAsync(new ParameterUpdateQueueRequest
        {
            CorrelationId = correlationId,
            DeviceId = request.DeviceId,
            SetupDeviceId = setupDevice.Id,
            ThingsBoardDeviceId = device.ThingsBoardDeviceId,
            Parameters = validParameters,
            UserId = _currentUser.Id,
            IsAdmin = isAdmin
        }, cancellationToken);

        _logger.LogInformation(
            "Queued {Count} parameter updates for device {DeviceId} with correlation ID {CorrelationId}",
            validParameters.Count,
            request.DeviceId,
            correlationId);

        // Return immediately with correlation ID
        // Frontend should subscribe to SignalR with this correlation ID to receive updates
        return new UpdateDeviceParametersResult
        {
            Success = true,
            CorrelationId = correlationId,
            TotalParameters = validParameters.Count,
            Message = $"Parameter update queued. Subscribe to SignalR hub 'deviceSettingsHub' with correlation ID '{correlationId}' to receive progress updates."
        };
    }
}

/// <summary>
/// Result of batch parameter update - returns immediately with correlation ID
/// </summary>
public record UpdateDeviceParametersResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Message { get; init; }

    /// <summary>
    /// Correlation ID to track this batch operation via SignalR
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Total number of parameters queued for update
    /// </summary>
    public int TotalParameters { get; init; }

    // Legacy properties for backward compatibility (will be empty for async operations)
    public List<ParameterUpdateStatus> SuccessfulUpdates { get; init; } = new();
    public List<ParameterUpdateStatus> FailedUpdates { get; init; } = new();
}

/// <summary>
/// Status of a single parameter update (for legacy compatibility)
/// </summary>
public record ParameterUpdateStatus
{
    public string ParameterName { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? RpcMethod { get; init; }
    public string? NewValue { get; init; }
}
